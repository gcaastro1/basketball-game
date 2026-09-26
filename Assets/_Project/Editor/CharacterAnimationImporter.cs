using System.IO;
using UnityEditor;

namespace Basket.EditorTools
{
    // Clip import settings for character animations (Assets/TripoModels/**/Animations/**):
    // loops for the cyclic ones and in-place motion, so gameplay alone moves the body.
    //
    // - Clips are renamed after their file (Mixamo exports every take as "mixamo.com") or
    //   without the "Armature|" prefix (Universal Animation Library).
    // - loopTime for idles, walks, runs, dribbles and "*_Loop" clips.
    // - Rotation and height baked into the pose; horizontal root motion left on the root,
    //   which the character's Animator never applies (applyRootMotion = false): in place.
    // Only fills clips that were never configured, so manual tweaks in the Inspector stay.
    public sealed class CharacterAnimationImporter : AssetPostprocessor
    {
        public const string Marker = "/Animations/";

        public override uint GetVersion() => 1;

        public static bool Handles(string path) =>
            path.StartsWith(TripoHumanoidImporter.TripoFolder) && path.Contains(Marker);

        private void OnPreprocessAnimation()
        {
            if (!Handles(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            if (importer.clipAnimations != null && importer.clipAnimations.Length > 0) return;
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            string file = Path.GetFileNameWithoutExtension(assetPath);
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = ClipName(clip.takeName, file, clips.Length);
                clip.loopTime = IsLoop(clip.name);
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = false;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
            }
            importer.clipAnimations = clips;
        }

        public static string ClipName(string takeName, string file, int takes)
        {
            if (takes == 1 && (string.IsNullOrEmpty(takeName) || takeName == "mixamo.com")) return file;
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        public static bool IsLoop(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.StartsWith("start") || n.Contains("_start") || n.Contains("_land") || n.Contains("_enter") || n.Contains("_exit"))
                return false;
            return n.EndsWith("_loop") || n.Contains("idle") || n.Contains("walk") || n.Contains("run")
                   || n.Contains("jog") || n.Contains("sprint") || n.Contains("dribble");
        }
    }
}
