using System.Collections.Generic;
using UnityEngine;

namespace Basket.Presentation
{
    // Imported art often uses the Built-in Standard shader, pink under URP. Each material is
    // rebuilt on the active pipeline's default lit material -- the one primitives get --
    // keeping its texture, colour, normal map, metal/smoothness and emission. One converted
    // copy per original material (shared across renderers, so a stadium stays batchable).
    public sealed class LitMaterials
    {
        private static Material litTemplate;
        private readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        private readonly Texture2D baseMapOverride;
        private readonly bool onlyUnsupported;

        // onlyUnsupported: keep materials whose shader already runs on this pipeline (e.g. a
        // URP asset); otherwise every material is rebuilt. baseMapOverride replaces every texture.
        public LitMaterials(bool onlyUnsupported, Texture2D baseMapOverride = null)
        {
            this.onlyUnsupported = onlyUnsupported;
            this.baseMapOverride = baseMapOverride;
        }

        public int ConvertedCount => converted.Count;

        public void Apply(GameObject root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] originals = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < originals.Length; i++)
                {
                    Material m = For(originals[i]);
                    if (m == originals[i]) continue;
                    originals[i] = m;
                    changed = true;
                }
                if (changed) r.sharedMaterials = originals;
            }
        }

        private Material For(Material original)
        {
            if (original != null && onlyUnsupported && !NeedsConversion(original)) return original;
            if (original != null && converted.TryGetValue(original, out Material done)) return done;
            Material m = Convert(original);
            if (original != null) converted[original] = m;
            return m;
        }

        public static bool NeedsConversion(Material m)
        {
            Shader s = m.shader;
            return s == null || !s.isSupported || s.name == "Standard" || s.name == "Standard (Specular setup)"
                || s.name.StartsWith("Legacy Shaders/");
        }

        private Material Convert(Material original)
        {
            if (litTemplate == null)
            {
                GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                litTemplate = probe.GetComponent<Renderer>().sharedMaterial;
                Object.DestroyImmediate(probe);
            }
            var material = new Material(litTemplate)
            {
                name = original != null ? original.name + " (Lit)" : "Lit",
                color = original != null && original.HasProperty("_Color") ? original.color : Color.white,
            };
            Texture texture = baseMapOverride != null ? baseMapOverride
                : original != null && original.HasProperty("_MainTex") ? original.mainTexture : null;
            if (texture != null) material.mainTexture = texture;
            if (original == null || baseMapOverride != null) return material;

            CopyTexture(original, "_BumpMap", material, "_BumpMap", "_NORMALMAP");
            CopyTexture(original, "_MetallicGlossMap", material, "_MetallicGlossMap", "_METALLICSPECGLOSSMAP");
            CopyTexture(original, "_OcclusionMap", material, "_OcclusionMap", "_OCCLUSIONMAP");
            CopyFloat(original, "_Metallic", material, "_Metallic");
            CopyFloat(original, "_Glossiness", material, "_Smoothness");
            if (original.IsKeywordEnabled("_EMISSION") && original.HasProperty("_EmissionColor") && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", original.GetColor("_EmissionColor"));
                CopyTexture(original, "_EmissionMap", material, "_EmissionMap", null);
                material.EnableKeyword("_EMISSION");
            }
            return material;
        }

        private static void CopyTexture(Material from, string fromName, Material to, string toName, string keyword)
        {
            if (!from.HasProperty(fromName) || !to.HasProperty(toName)) return;
            Texture t = from.GetTexture(fromName);
            if (t == null) return;
            to.SetTexture(toName, t);
            if (keyword != null) to.EnableKeyword(keyword);
        }

        private static void CopyFloat(Material from, string fromName, Material to, string toName)
        {
            if (from.HasProperty(fromName) && to.HasProperty(toName)) to.SetFloat(toName, from.GetFloat(fromName));
        }
    }
}
