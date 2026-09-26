using UnityEngine;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // Dresses the gameplay arena with art: builds the stadium around the court and swaps the
    // placeholder ball sphere for a model. Only renderers change; colliders stay the
    // placeholder ones, so physics and every simulation are the same with or without art.
    public static class ArenaDresser
    {
        public const string StadiumName = "Stadium";
        public const string BallModelName = "BallModel";

        public static GameObject Dress(Arena arena, CourtConfig court, ArenaVisualDefinition def)
        {
            if (def == null) return null;
            GameObject stadium = null;
            if (def.stadium != null && def.stadium.pieces.Count > 0)
            {
                stadium = BuildStadium(def.stadium, court.FullCourtCenter, arena.Root.transform);
                if (def.hidePlaceholderCourt) HidePlaceholderCourt(arena.Root.transform);
            }
            if (def.ballModel != null) DressBall(arena.Ball.gameObject, def.ballModel, def.ballDiameter);
            return stadium;
        }

        public static GameObject BuildStadium(StadiumLayout layout, Vector3 courtCenter, Transform parent)
        {
            var root = new GameObject(StadiumName);
            root.transform.SetParent(parent, false);
            root.transform.position = courtCenter;
            var materials = new LitMaterials(onlyUnsupported: true);
            foreach (StadiumLayout.Piece piece in layout.pieces)
            {
                if (piece.prefab == null) continue;
                GameObject go = Object.Instantiate(piece.prefab, root.transform, false);
                go.transform.localPosition = piece.position;
                go.transform.localRotation = piece.rotation;
                go.transform.localScale = piece.scale;
                OverrideMaterials(go, piece.materials);
                StripPhysics(go);
                materials.Apply(go);
                // Decor casts no shadows: the stadium roof would otherwise shade the whole court
                // from the arena's directional light.
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return root;
        }

        public static GameObject DressBall(GameObject ball, GameObject model, float diameter)
        {
            GameObject visual = Object.Instantiate(model, ball.transform, false);
            visual.name = BallModelName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            StripPhysics(visual);
            new LitMaterials(onlyUnsupported: true).Apply(visual);

            if (TryBounds(visual, out Bounds b) && b.size.x > 0.0001f)
            {
                float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                Vector3 s = visual.transform.localScale * (diameter / size);
                visual.transform.localScale = s;
                if (TryBounds(visual, out b)) visual.transform.position += ball.transform.position - b.center;
            }
            if (ball.TryGetComponent<Renderer>(out var sphere)) sphere.enabled = false;
            return visual;
        }

        private static void OverrideMaterials(GameObject go, Material[] materials)
        {
            if (materials == null || materials.Length == 0) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r == null) r = go.GetComponentInChildren<Renderer>(true);
            if (r == null) return;
            Material[] slots = r.sharedMaterials;
            for (int i = 0; i < materials.Length && i < slots.Length; i++)
            {
                if (materials[i] != null) slots[i] = materials[i];
            }
            r.sharedMaterials = slots;
        }

        private static void HidePlaceholderCourt(Transform arenaRoot)
        {
            foreach (Transform child in arenaRoot)
            {
                if (child.name == "CourtFloor" && child.TryGetComponent<Renderer>(out var r)) r.enabled = false;
                else if (child.name == "ThreePointLine" || child.name == "CenterMarks") child.gameObject.SetActive(false);
            }
        }

        private static void StripPhysics(GameObject go)
        {
            foreach (Rigidbody rb in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        }

        private static bool TryBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}
