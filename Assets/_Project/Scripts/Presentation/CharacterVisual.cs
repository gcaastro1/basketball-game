using UnityEngine;
using Basket.Characters;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // Replaces a player's placeholder capsule with a character model (Etapa 6). The
    // gameplay body -- CharacterController, motor, entity -- is untouched: the model is a
    // child that only looks. Nothing in gameplay reads it.
    public class CharacterVisual : MonoBehaviour
    {
        private static Material litTemplate;

        public GameObject Model { get; private set; }
        public PlayerAnimationDriver Driver { get; private set; }

        public static CharacterVisual Attach(PlayerEntity player, MatchSimulation sim, CharacterVisualDefinition definition, Color teamColor)
        {
            if (player == null || definition == null || definition.modelPrefab == null) return null;
            var visual = player.gameObject.AddComponent<CharacterVisual>();
            visual.Build(player, sim, definition, teamColor);
            return visual;
        }

        private void Build(PlayerEntity player, MatchSimulation sim, CharacterVisualDefinition def, Color teamColor)
        {
            Transform root = player.transform;
            Model = Instantiate(def.modelPrefab, root, false);
            Model.name = "Model";
            Transform model = Model.transform;
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.Euler(0f, def.yawOffsetDegrees, 0f);
            model.localScale = Vector3.one;
            // Gameplay collision is the root CharacterController only.
            foreach (Collider c in Model.GetComponentsInChildren<Collider>()) DestroyImmediate(c);

            if (def.overrideMaterials) ApplyMaterial(Model, def.baseMap);
            float feetLocalY = root.InverseTransformPoint(player.FeetPosition).y;
            FitToHeight(model, def.modelHeight, feetLocalY);
            HidePlaceholder(root);
            AddTeamRing(root, feetLocalY, teamColor);

            Animator animator = Model.GetComponentInChildren<Animator>();
            if (animator == null) animator = Model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (SkinnedMeshRenderer skin in Model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.updateWhenOffscreen = true; // bounds follow the procedural pose
            }
            Driver = Model.AddComponent<PlayerAnimationDriver>();
            Driver.Configure(player, sim, animator, def.clips, SoleBelowToes(Model, animator));
        }

        private static void FitToHeight(Transform model, float height, float feetLocalY)
        {
            if (!MeasureMeshY(model.gameObject, out float minY, out float maxY)) return;
            float originY = model.position.y;
            ModelFitting.Fit(minY - originY, maxY - originY, height, out float scale, out float yOffset);
            model.localScale = Vector3.one * scale;
            model.localPosition = new Vector3(0f, feetLocalY + yOffset, 0f);
        }

        // Lowest and highest point of the model's actual (skinned, posed) mesh in world
        // space. Renderer bounds of a skinned mesh are only an estimate from the bind pose;
        // the Tripo model's were off by more than a meter.
        public static bool MeasureMeshY(GameObject model, out float minY, out float maxY)
        {
            minY = float.MaxValue;
            maxY = float.MinValue;
            var baked = new Mesh();
            foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                // Baked in the renderer's unscaled local frame; its full localToWorld (with
                // every parent's scale -- the fitted model's) takes it to the world. Baking
                // "with scale" only applied the renderer's own scale: CI measured a 1.85 m
                // model as 1.00 m.
                skin.BakeMesh(baked, false);
                Transform t = skin.transform;
                foreach (Vector3 v in baked.vertices) Extend(t.TransformPoint(v), ref minY, ref maxY);
            }
            Destroy(baked);
            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                Transform t = filter.transform;
                foreach (Vector3 v in filter.sharedMesh.vertices) Extend(t.TransformPoint(v), ref minY, ref maxY);
            }
            return minY <= maxY;
        }

        private static void Extend(Vector3 p, ref float minY, ref float maxY)
        {
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        // How far the sole of the mesh is below the lowest toe (or ankle) bone, so the driver
        // can put the sole -- not the joint -- on the floor.
        private static float SoleBelowToes(GameObject model, Animator animator)
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman) return 0f;
            Transform l = animator.GetBoneTransform(HumanBodyBones.LeftToes) ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform r = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (l == null || r == null || !MeasureMeshY(model, out float minY, out _)) return 0f;
            return Mathf.Clamp(Mathf.Min(l.position.y, r.position.y) - minY, 0f, 0.3f);
        }

        // Tripo's generated material uses the Built-in Standard shader (pink under URP): use
        // the active pipeline's default lit material -- the one primitives get -- with the
        // model's texture.
        private static void ApplyMaterial(GameObject model, Texture2D baseMap)
        {
            if (litTemplate == null)
            {
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                litTemplate = probe.GetComponent<Renderer>().sharedMaterial;
                DestroyImmediate(probe);
            }
            var material = new Material(litTemplate) { name = "CharacterLit", color = Color.white };
            if (baseMap != null) material.mainTexture = baseMap;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                var shared = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < shared.Length; i++) shared[i] = material;
                r.sharedMaterials = shared;
            }
        }

        private static void HidePlaceholder(Transform root)
        {
            foreach (string name in new[] { "Body", "Facing" })
            {
                Transform t = root.Find(name);
                if (t != null && t.TryGetComponent<Renderer>(out var r)) r.enabled = false;
            }
        }

        // Keeps the teams readable while every character shares one placeholder model.
        private static void AddTeamRing(Transform root, float feetLocalY, Color color)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "TeamRing";
            DestroyImmediate(ring.GetComponent<Collider>());
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0f, feetLocalY + 0.01f, 0f);
            ring.transform.localScale = new Vector3(0.9f, 0.005f, 0.9f);
            ring.GetComponent<Renderer>().material.color = color;
        }
    }
}
