using System.Collections;
using System.Text;
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

        GameObject stadium = ArenaDresser.Dress(arena, court, def);
        yield return null;

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

    // The stadium's own hoop (Ring.fbx + Net + padded pole) is left out for now: gameplay
    // needs its rim exactly at the rim collider. These numbers are to fit it later.
    [Test]
    public void StadiumHoopModel_Measurements()
    {
        var log = new StringBuilder("Stadium hoop parts:\n");
        foreach (string path in new[] { "Assets/MarpaStudio/Mesh/Ring.fbx", "Assets/MarpaStudio/Built-In/Prefabs/Net.prefab",
                     "Assets/MarpaStudio/Built-In/Prefabs/FoamPoleFinal.prefab", "Assets/MarpaStudio/Built-In/Prefabs/FoamFinal.prefab" })
        {
            var prefab = Load<GameObject>(path);
            GameObject go = Object.Instantiate(prefab);
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = mf.sharedMesh;
                log.AppendLine($"  {path} / {mf.name}: world bounds {mf.GetComponent<Renderer>().bounds}, root rot {go.transform.rotation.eulerAngles}, local rot {mf.transform.localEulerAngles}, scale {mf.transform.lossyScale}");
                for (int s = 0; mesh != null && s < mesh.subMeshCount; s++)
                    log.AppendLine($"    submesh {s}: {mesh.GetSubMesh(s).bounds}");
            }
            Object.DestroyImmediate(go);
        }
        Debug.Log(log.ToString());
    }
}
