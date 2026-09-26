using System.IO;
using UnityEditor;

namespace Basket.EditorTools
{
    // Clip import settings for character animations (Assets/TripoModels/**/Animations/**):
    // loops for the cyclic ones and in-place motion, so gameplay alone moves the body.
    //
    // - A file with one take gets the file's name (Mixamo calls every take "mixamo.com", the
    //   basketball mocap "CharacterArmature|124_05_remap"); multi-take files keep the take
    //   name without its "Armature|" prefix (Universal Animation Library).
    // - loopTime for idles, walks, runs, dribbles, defensive slides and "*_Loop" clips, never
    //   for shots, starts, landings or turns.
    // - Rotation and height baked into the pose; horizontal root motion left on the root,
    //   which the character's Animator never applies (applyRootMotion = false): in place.
    // - Frame range = the file's real motion (FbxCurveSpan): the mocap takes declare far longer
    //   spans than their curves (a 0.93 s run inside a 88.5 s take). Their first frame is a
    //   calibration pose ("*_remap" takes), skipped. A range past the curves is always trimmed.
    // - 30 fps drop-frame files are fixed first (FbxFrameRateFixer), so frames are 1/30 s.
    // Other settings are only filled for clips never configured, so Inspector tweaks stay.
    public sealed class CharacterAnimationImporter : AssetPostprocessor
    {
        public const float FramesPerSecond = 30f;

        public const string Marker = "/Animations/";

        public override uint GetVersion() => 4;

        public static bool Handles(string path) =>
            path.StartsWith(TripoHumanoidImporter.TripoFolder) && path.Contains(Marker);

        private void OnPreprocessModel()
        {
            if (!Handles(assetPath) || !File.Exists(assetPath)) return;
            byte[] data = File.ReadAllBytes(assetPath);
            if (FbxFrameRateFixer.TryFix(data)) File.WriteAllBytes(assetPath, data);
        }

        private void OnPreprocessAnimation()
        {
            if (!Handles(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            bool configured = importer.clipAnimations != null && importer.clipAnimations.Length > 0;
            ModelImporterClipAnimation[] clips = configured ? importer.clipAnimations : importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;
            bool changed = TrimToMotion(clips);
            if (configured)
            {
                ModelImporterClipAnimation[] withMirror = AddMirror(clips);
                if (changed || withMirror != clips) importer.clipAnimations = withMirror;
                return;
            }
            string file = Path.GetFileNameWithoutExtension(assetPath);
            foreach (ModelImporterClipAnimation clip in clips)
            {
                clip.name = ClipName(clip.takeName, file, clips.Length);
                clip.loopTime = IsLoop(clip.name);
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = false;
                clip.keepOriginalOrientation = !IsMocap(clip.takeName);
                clip.keepOriginalPositionY = true;
            }
            importer.clipAnimations = AddMirror(clips);
        }

        public const string MirrorSuffix = "_Mirror";

        // Mocap takes also come mirrored ("<name>_Mirror"): a slide or turn to one side gives
        // the other side too.
        private static ModelImporterClipAnimation[] AddMirror(ModelImporterClipAnimation[] clips)
        {
            if (clips.Length != 1 || !IsMocap(clips[0].takeName)) return clips;
            ModelImporterClipAnimation c = clips[0];
            var mirror = new ModelImporterClipAnimation
            {
                name = c.name + MirrorSuffix,
                takeName = c.takeName,
                firstFrame = c.firstFrame,
                lastFrame = c.lastFrame,
                loopTime = c.loopTime,
                lockRootRotation = c.lockRootRotation,
                lockRootHeightY = c.lockRootHeightY,
                lockRootPositionXZ = c.lockRootPositionXZ,
                keepOriginalOrientation = c.keepOriginalOrientation,
                keepOriginalPositionY = c.keepOriginalPositionY,
                mirror = true,
            };
            return new[] { c, mirror };
        }

        // Mocap takes ("*_remap", 30 fps once fixed): the clip covers the curves only, minus the
        // calibration frame.
        // Always reset: ranges saved while the file still read as 1 fps are meaningless, and
        // narrowing a clip is done with the character's clip windows, not here.
        private bool TrimToMotion(ModelImporterClipAnimation[] clips)
        {
            if (clips.Length == 0 || clips.Length > 2 || !IsMocap(clips[0].takeName)) return false;
            if (!FbxCurveSpan.TryRead(assetPath, out double start, out double end)) return false;
            (float first, float last) = FrameRange(start, end, skipCalibration: true);
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                // Each recording faces its own way (the run heads +X, the dribble +Z): the root
                // rotation follows the body so every clip faces forward -- based on the original
                // orientation, the character ran sideways.
                bool oriented = clip.lockRootRotation && !clip.keepOriginalOrientation;
                if (first == clip.firstFrame && last == clip.lastFrame && oriented) continue;
                clip.firstFrame = first;
                clip.lastFrame = last;
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = false;
                changed = true;
            }
            return changed;
        }

        public static bool IsMocap(string takeName) => takeName != null && takeName.EndsWith("_remap");

        // Frames (at 30 fps) covering curves from start to end seconds.
        public static (float first, float last) FrameRange(double start, double end, bool skipCalibration)
        {
            float first = (float)System.Math.Round(start * FramesPerSecond) + (skipCalibration ? 1f : 0f);
            float last = (float)System.Math.Round(end * FramesPerSecond);
            return (first, last > first ? last : first + 1f);
        }

        public static string ClipName(string takeName, string file, int takes)
        {
            if (takes == 1 || string.IsNullOrEmpty(takeName)) return file;
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        public static bool IsLoop(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.StartsWith("start") || n.Contains("_start") || n.Contains("_land") || n.Contains("_enter") || n.Contains("_exit")
                || n.Contains("shoot") || n.Contains("shot") || n.Contains("throw") || n.Contains("lay_up") || n.Contains("layup")
                || n.Contains("turn") || n.Contains("stop") || n.Contains("fake") || n.Contains("feint") || n.Contains("spin"))
                return false;
            return n.EndsWith("_loop") || n.Contains("idle") || n.Contains("walk") || n.Contains("run")
                   || n.Contains("jog") || n.Contains("sprint") || n.Contains("dribble") || n.Contains("defensivemove");
        }
    }
}
