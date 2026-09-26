using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;
using Basket.Presentation;

// Etapa 6.6: the stadium (MarpaStudio) around the court and the ball model (TierrasDeRol)
// are art only -- the NBA court drawn on the stadium floor lines up with the gameplay court,
// and physics keeps the placeholder colliders.
public class ArenaVisualTests
{
    private const string Data = "Assets/_Project/Data/";

    private static T Load<T>(string path) where T : Object
    {
#if UNITY_EDITOR
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
#else
        Assert.Ignore("needs the editor asset database");
        return null;
#endif
    }

    private GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.Destroy(root);
    }

    [Test]
    public void StadiumLayout_EveryPiecePointsAtAModel()
    {
        var layout = Load<StadiumLayout>(Data + "Arena/MarpaStadiumLayout.asset");
        Assert.Greater(layout.pieces.Count, 1000);
        int missing = 0;
        foreach (StadiumLayout.Piece p in layout.pieces) if (p.prefab == null) missing++;
        Assert.AreEqual(0, missing, "pieces without a prefab (broken reference)");
    }

    [UnityTest]
    public IEnumerator FullCourt_StadiumFloorLinesUpWithTheCourt_BallModelFits_PhysicsUntouched()
    {
        var court = Load<CourtConfig>(Data + "Court5v5Config.asset");
        var rules = Load<MatchRules>(Data + "FIBA5v5MatchRules.asset");
        var def = Load<ArenaVisualDefinition>(Data + "Arena/DefaultArenaVisual.asset");
        Arena arena = PlaceholderArenaBuilder.Build(court, ScriptableObject.CreateInstance<BallConfig>(),
            rules.threePointRadius, rules.threePointCornerDistance);
        root = arena.Root;
        arena.Ball.GetComponent<Rigidbody>().isKinematic = true;

        int collidersBefore = arena.Root.GetComponentsInChildren<Collider>(true).Length;
        GameObject stadium = ArenaDresser.Dress(arena, court, def);
        yield return null;

        Assert.AreEqual(collidersBefore, arena.Root.GetComponentsInChildren<Collider>(true).Length, "dressing adds no colliders");
        foreach (HoopController hoop in new[] { arena.Hoop, arena.SecondHoop })
            AssertHoopDressed(hoop, court);
        Assert.AreEqual(4, CountNamed(arena.Root.transform, ArenaDresser.StanchionName), "base + post behind each board");
        foreach (Transform t in arena.Root.GetComponentsInChildren<Transform>())
        {
            if (t.name != ArenaDresser.StanchionName) continue;
            Assert.Greater(Mathf.Abs(t.position.z - court.FullCourtCenter.z), court.depth * 0.5f, "stanchion behind the baseline");
        }
        foreach (Transform t in arena.Root.transform)
            if (t.name == "Backboard")
                Assert.GreaterOrEqual(t.GetComponent<Renderer>().sharedMaterial.renderQueue, 3000, "glass backboard");

        Assert.IsNotNull(stadium);
        Assert.AreEqual(0, stadium.GetComponentsInChildren<Collider>(true).Length, "the stadium adds no colliders");
        Transform floor = arena.Root.transform.Find("CourtFloor");
        Assert.IsTrue(floor.GetComponent<Collider>().enabled, "gameplay floor collider stays");
        Assert.IsFalse(floor.GetComponent<Renderer>().enabled, "placeholder floor hidden");

        // The court drawn on the stadium floor = the gameplay court (NBA, 15.24 x 28.65 m).
        Renderer field = null;
        foreach (Renderer r in stadium.GetComponentsInChildren<Renderer>())
            if (r.gameObject.name.StartsWith("PlayField")) field = r;
        Assert.IsNotNull(field, "PlayField piece");
        Bounds fb = field.bounds;
        Debug.Log($"Stadium: {stadium.transform.childCount} pieces; PlayField bounds center {fb.center} size {fb.size}; court {court.width} x {court.depth}");
        Assert.AreEqual(court.width, fb.size.x, 0.1f, "floor width along x");
        Assert.AreEqual(court.depth, fb.size.z, 0.1f, "floor length along z");
        Assert.AreEqual(court.FullCourtCenter.x, fb.center.x, 0.05f);
        Assert.AreEqual(court.FullCourtCenter.z, fb.center.z, 0.05f);
        Assert.AreEqual(0f, fb.max.y, 0.03f, "floor at y = 0");

        // Every material renders on URP (none left on the Built-in Standard shader).
        int unsupported = 0;
        foreach (Renderer r in stadium.GetComponentsInChildren<Renderer>(true))
            foreach (Material m in r.sharedMaterials)
                if (m != null && LitMaterials.NeedsConversion(m)) unsupported++;
        Assert.AreEqual(0, unsupported, "materials still on an unsupported shader");

        // Ball model: regulation size, centered on the physics ball, sphere hidden.
        Transform model = arena.Ball.transform.Find(ArenaDresser.BallModelName);
        Assert.IsNotNull(model);
        Bounds bb = default;
        bool any = false;
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            if (!any) { bb = r.bounds; any = true; } else bb.Encapsulate(r.bounds);
        }
        Assert.IsTrue(any);
        Debug.Log($"Ball model bounds size {bb.size}, offset {(bb.center - arena.Ball.transform.position).magnitude:0.000} m");
        Assert.AreEqual(def.ballDiameter, Mathf.Max(bb.size.x, bb.size.y, bb.size.z), 0.01f);
        Assert.Less((bb.center - arena.Ball.transform.position).magnitude, 0.01f);
        Assert.IsFalse(arena.Ball.GetComponent<Renderer>().enabled, "placeholder sphere hidden");
        Assert.AreEqual(0, model.GetComponentsInChildren<Collider>(true).Length);
        Assert.AreEqual(1, arena.Ball.GetComponentsInChildren<Rigidbody>(true).Length, "only the physics body");
    }

    // The net hangs from the gameplay rim: as wide as the rim, top at rim height, centered.
    private static void AssertHoopDressed(HoopController hoop, CourtConfig court)
    {
        Transform net = hoop.transform.Find(ArenaDresser.NetName);
        Assert.IsNotNull(net, "net under " + hoop.name);
        Bounds b = default;
        bool any = false;
        foreach (Renderer r in net.GetComponentsInChildren<Renderer>())
        {
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        Assert.IsTrue(any);
        Vector3 rim = hoop.transform.position;
        Debug.Log($"Net at {rim}: width {Mathf.Max(b.size.x, b.size.z):0.000} m, length {b.size.y:0.000} m, top {b.max.y:0.000} m");
        Assert.AreEqual(2f * court.rimRadius, Mathf.Max(b.size.x, b.size.z), 0.01f);
        Assert.AreEqual(rim.y, b.max.y, 0.01f);
        Assert.Less(new Vector2(b.center.x - rim.x, b.center.z - rim.z).magnitude, 0.01f);
    }

    private static int CountNamed(Transform root, string name)
    {
        int n = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) n++;
        return n;
    }
}
