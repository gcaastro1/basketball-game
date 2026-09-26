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
        public const string NetName = "Net";
        public const string StanchionName = "Stanchion";

        public static GameObject Dress(Arena arena, CourtConfig court, ArenaVisualDefinition def)
        {
            if (def == null) return null;
            GameObject stadium = null;
            if (def.stadium != null && def.stadium.pieces.Count > 0)
            {
                stadium = BuildStadium(def.stadium, court.FullCourtCenter, arena.Root.transform);
                if (def.hidePlaceholderCourt) HidePlaceholderCourt(arena.Root.transform);
            }
            DressHoop(arena, arena.Hoop, court, def);
            if (arena.SecondHoop != null) DressHoop(arena, arena.SecondHoop, court, def);
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

        // Dresses one basket around the gameplay rim, which stays where it is (the stadium's own
        // hoop model has an oversized rim below 3.05 m, so only its parts are reused): a net
        // scaled to the rim, a glass backboard with its lines, a bracket, and a padded stanchion
        // behind the board with an arm up to it. Visual only: no colliders are added.
        public static void DressHoop(Arena arena, HoopController hoop, CourtConfig court, ArenaVisualDefinition def)
        {
            Vector3 rim = hoop.transform.position;
            Transform board = NearestBoard(arena.Root.transform, rim);
            var materials = new LitMaterials(onlyUnsupported: true);
            var parent = new GameObject("HoopDressing").transform;
            parent.SetParent(arena.Root.transform, false);

            if (def.netModel != null)
            {
                GameObject net = Object.Instantiate(def.netModel, hoop.transform, false);
                net.name = NetName;
                StripPhysics(net);
                materials.Apply(net);
                FitNet(net, rim, court.rimRadius, def.netLengthRatio);
            }
            if (board == null) return;

            Vector3 boardCenter = board.position;
            Vector3 size = board.localScale;
            float back = Mathf.Sign(boardCenter.z - rim.z);   // from the rim toward the board
            if (def.backboardMaterial != null && board.TryGetComponent<Renderer>(out var boardRenderer))
                boardRenderer.sharedMaterial = materials.Get(def.backboardMaterial);

            // Lines on the court side of the glass: the border and the shooter's square
            // (0.61 x 0.457 m, bottom edge level with the rim).
            float faceZ = boardCenter.z - back * (size.z * 0.5f + 0.003f);
            const float line = 0.05f;
            float top = boardCenter.y + size.y * 0.5f, bottom = boardCenter.y - size.y * 0.5f;
            float left = boardCenter.x - size.x * 0.5f, right = boardCenter.x + size.x * 0.5f;
            Bar(parent, "BoardLine", new Vector3(boardCenter.x, top - line * 0.5f, faceZ), new Vector3(size.x, line, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(boardCenter.x, bottom + line * 0.5f, faceZ), new Vector3(size.x, line, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(left + line * 0.5f, boardCenter.y, faceZ), new Vector3(line, size.y, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(right - line * 0.5f, boardCenter.y, faceZ), new Vector3(line, size.y, 0.004f), def.backboardLineColor);
            const float squareW = 0.61f, squareH = 0.457f;
            float squareBottom = rim.y, squareTop = rim.y + squareH;
            Bar(parent, "BoardLine", new Vector3(rim.x, squareTop - line * 0.5f, faceZ), new Vector3(squareW, line, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(rim.x, squareBottom + line * 0.5f, faceZ), new Vector3(squareW, line, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(rim.x - (squareW - line) * 0.5f, (squareTop + squareBottom) * 0.5f, faceZ), new Vector3(line, squareH, 0.004f), def.backboardLineColor);
            Bar(parent, "BoardLine", new Vector3(rim.x + (squareW - line) * 0.5f, (squareTop + squareBottom) * 0.5f, faceZ), new Vector3(line, squareH, 0.004f), def.backboardLineColor);

            // Bracket from the glass to the back of the rim.
            float rimBack = rim.z + back * court.rimRadius;
            Bar(parent, "RimBracket", new Vector3(rim.x, rim.y - 0.03f, (rimBack + faceZ) * 0.5f),
                new Vector3(0.14f, 0.06f, Mathf.Abs(faceZ - rimBack)), def.stanchionArmColor);

            // Stanchion: padded base + post behind the board, arm up and over to the glass.
            Vector3 foot = new Vector3(rim.x, 0f, boardCenter.z + back * def.stanchionDistance);
            float postTop = 0f;
            foreach (GameObject model in new[] { def.stanchionBase, def.stanchionPost })
            {
                if (model == null) continue;
                GameObject part = Object.Instantiate(model, parent, false);
                part.name = StanchionName;
                part.transform.position = foot;
                StripPhysics(part);
                materials.Apply(part);
                if (TryBounds(part, out Bounds pb)) postTop = Mathf.Max(postTop, pb.max.y);
            }
            float boardBack = boardCenter.z + back * size.z * 0.5f;
            const float arm = 0.22f;
            float armY = boardCenter.y;
            Bar(parent, "StanchionArm", new Vector3(foot.x, (postTop + armY + arm * 0.5f) * 0.5f, foot.z),
                new Vector3(arm, armY + arm * 0.5f - postTop, arm), def.stanchionArmColor);
            Bar(parent, "StanchionArm", new Vector3(foot.x, armY, (foot.z + boardBack) * 0.5f),
                new Vector3(arm, arm, Mathf.Abs(foot.z - boardBack)), def.stanchionArmColor);
        }

        // Uniform scale so the net's width is the rim's diameter, hung from the rim.
        public static void FitNet(GameObject net, Vector3 rim, float rimRadius, float lengthRatio)
        {
            net.transform.localPosition = Vector3.zero;
            net.transform.localRotation = Quaternion.identity;
            net.transform.localScale = Vector3.one;
            if (!TryBounds(net, out Bounds b)) return;
            float width = Mathf.Max(b.size.x, b.size.z);
            if (width < 0.0001f) return;
            float k = 2f * rimRadius / width;
            Vector3 local = net.transform.localScale * k;
            if (lengthRatio > 0f && b.size.y > 0.0001f)
                local.y = net.transform.localScale.y * (2f * rimRadius * lengthRatio / b.size.y);
            net.transform.localScale = local;
            if (TryBounds(net, out b))
                net.transform.position += new Vector3(rim.x - b.center.x, rim.y - b.max.y, rim.z - b.center.z);
        }

        private static Transform NearestBoard(Transform arenaRoot, Vector3 rim)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (Transform child in arenaRoot)
            {
                if (child.name != "Backboard") continue;
                float d = (child.position - rim).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = child; }
            }
            return best;
        }

        private static void Bar(Transform parent, string name, Vector3 position, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().material.color = color;
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
