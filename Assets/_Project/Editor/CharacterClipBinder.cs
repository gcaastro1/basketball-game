using System.Linq;
using UnityEditor;
using UnityEngine;
using Basket.Characters;

namespace Basket.EditorTools
{
    // Binds the default character's clips to the basketball mocap set in
    // Assets/TripoModels/anime_character_3d_model/Animations/Basquete -- only those. Clip
    // references inside an FBX carry Unity-generated ids, so they are bound here, in the
    // editor, instead of by hand in the asset file.
    //
    // Windows are given in seconds of the original recording (measured on the files' curves:
    // still stretches from hip speed, shot gather/release from hip height) and turned into
    // the clip's normalized time here: the importer trims each take to its motion and skips
    // its first (calibration) frame.
    //
    // Runs after those files import and once per editor load, filling a slot only while it is
    // empty -- anything set in the Inspector wins. Menu: Basket -> Bind Character Animations.
    public sealed class CharacterClipBinder : AssetPostprocessor
    {
        public const string VisualPath = "Assets/_Project/Data/Characters/DefaultCharacterVisual.asset";
        public const string Folder = TripoHumanoidImporter.TripoFolder + "anime_character_3d_model/Animations/Basquete/";
        // The importer drops the first frame (1/30 s) of every mocap take.
        private const float CalibrationFrame = 1f / CharacterAnimationImporter.FramesPerSecond;

        [MenuItem("Basket/Bind Character Animations")]
        public static void BindMenu()
        {
            int bound = Bind();
            Debug.Log($"Character animations: {bound} clip slot(s) filled on {VisualPath}.");
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad() => EditorApplication.delayCall += () => Bind();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Any(p => CharacterAnimationImporter.Handles(p) || p == VisualPath))
                EditorApplication.delayCall += () => Bind();
        }

        // Returns how many slots were filled.
        public static int Bind()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return 0;
            var visual = AssetDatabase.LoadAssetAtPath<CharacterVisualDefinition>(VisualPath);
            if (visual == null) return 0;
            CharacterAnimationClips c = visual.clips ??= new CharacterAnimationClips();
            c.free ??= new LocomotionClipSet();
            c.withBall ??= new LocomotionClipSet();
            c.guard ??= new LocomotionClipSet();
            int n = 0;

            // Without the ball: standing (a quiet stretch of a signals take), running and its turns.
            n += Loop(ref c.free.idle, "basketball_signals_32_07", 18.0f, 20.0f);
            n += Loop(ref c.free.run, "RunningStraight_102_05");
            n += Loop(ref c.free.runTurnLeft, "RunningWideLeft_102_07");
            n += Loop(ref c.free.runTurnRight, "RunningWideRight_102_06");
            n += Speeds(c.free, 1.5f, 4f);

            // With the ball: holding it (free-throw routine before the shot), dribbling in every
            // direction at walking pace, running legs + upper-body dribble above that.
            n += Loop(ref c.withBall.idle, "Basketball_Free_Throw_124_04", 0.1f, 0.95f);
            n += Loop(ref c.withBall.forward, "basketball_forward_dribble_06_04", 0.5f, 2.8f);
            n += Loop(ref c.withBall.backward, "basketball_backward_dribble_06_06", 0.5f, 2.9f);
            n += Loop(ref c.withBall.right, "basketball_sideways_dribble_06_08", 0.4f, 2.5f);
            n += Loop(ref c.withBall.left, "basketball_sideways_dribble_06_08", 0.4f, 2.5f, mirror: true);
            n += Loop(ref c.withBall.run, "RunningStraight_102_05");
            n += Loop(ref c.withBall.runTurnLeft, "RunningWideLeft_102_07");
            n += Loop(ref c.withBall.runTurnRight, "RunningWideRight_102_06");
            n += Speeds(c.withBall, 1.3f, 4f);
            n += Loop(ref c.dribbleUpperBody, "basketball_forward_dribble_06_04", 0.5f, 2.8f);

            // Defensive guard: the stance (first frame of a stop-to-stop slide), side slides both
            // ways, forward shuffle (backwards for backing up).
            n += Loop(ref c.guard.idle, "DefensiveLeftStopToStop_102_25", 0.05f, 0.05f);
            n += Loop(ref c.guard.right, "DefensiveMoveSideToSide_102_27", 0.1f, 1.35f);
            n += Loop(ref c.guard.left, "DefensiveMoveSideToSide_102_27", 0.1f, 1.35f, mirror: true);
            n += Loop(ref c.guard.forward, "DefensiveStraightNoStop_102_22", 0.3f, 1.8f);
            n += Loop(ref c.guard.backward, "DefensiveStraightNoStop_102_22", 0.3f, 1.8f, reverse: true);
            n += Speeds(c.guard, 2.5f, 4f);

            // Shots: gather (crouch before the jump) -> release (top of the jump).
            n += Actions(ref c.jumpShots,
                Action("Basketball_Jump_Shot_124_05", 3.03f, 3.43f),
                Action("Basketball_Shoot_124_03", 3.83f, 4.17f));
            n += Actions(ref c.jumpShotsMoving,
                Action("basketball_dribble_shoot_06_15", 2.54f, 2.90f),
                Action("basketball_crossover_dribble_shoot_06_14", 2.10f, 2.40f));
            n += One(ref c.layup, Action("Basketball_Lay_Up_124_06", 2.90f, 3.43f));
            n += One(ref c.dunk, Action("Basketball_Lay_Up_124_06", 2.90f, 3.43f));
            n += One(ref c.freeThrow, Action("Basketball_Free_Throw_124_04", 4.07f, 4.47f));
            // Jumps without a shot (rebound, block): the jump of the jump shot, up and down.
            n += One(ref c.airborne, Action("Basketball_Jump_Shot_124_05", 3.03f, 3.9f));
            n += One(ref c.block, Action("Basketball_Jump_Shot_124_05", 3.03f, 3.9f));

            if (n > 0)
            {
                EditorUtility.SetDirty(visual);
                AssetDatabase.SaveAssets();
            }
            return n;
        }

        private static int Loop(ref LoopClip slot, string file, float fromSeconds = -1f, float toSeconds = -1f,
            bool mirror = false, bool reverse = false)
        {
            if (slot.clip != null) return 0;
            AnimationClip clip = FindClip(Folder + file + ".fbx", mirror ? file + CharacterAnimationImporter.MirrorSuffix : file);
            if (clip == null || !Trimmed(Folder + file + ".fbx", clip)) return 0;
            slot = fromSeconds < 0f
                ? new LoopClip(clip, 0f, 1f, reverse)
                : new LoopClip(clip, Normalized(clip, fromSeconds), Normalized(clip, toSeconds), reverse);
            return 1;
        }

        private static ActionClip Action(string file, float startSeconds, float releaseSeconds)
        {
            AnimationClip clip = FindClip(Folder + file + ".fbx", file);
            if (clip == null || !Trimmed(Folder + file + ".fbx", clip)) return default;
            return new ActionClip(clip, new ClipWindow(Normalized(clip, startSeconds), Normalized(clip, releaseSeconds)));
        }

        private static int One(ref ActionClip slot, ActionClip bound)
        {
            if (slot.clip != null || bound.clip == null) return 0;
            slot = bound;
            return 1;
        }

        private static int Actions(ref ActionClip[] slot, params ActionClip[] bound)
        {
            if (slot != null && slot.Length > 0 && slot.All(a => a.clip != null)) return 0;
            ActionClip[] found = bound.Where(a => a.clip != null).ToArray();
            if (found.Length == 0) return 0;
            slot = found;
            return 1;
        }

        // Clip speeds of a set, only while it still has the class defaults.
        private static int Speeds(LocomotionClipSet set, float move, float run)
        {
            var defaults = new LocomotionClipSet();
            if (set.moveSpeed != defaults.moveSpeed || set.runSpeed != defaults.runSpeed) return 0;
            if (set.moveSpeed == move && set.runSpeed == run) return 0;
            set.moveSpeed = move;
            set.runSpeed = run;
            return 1;
        }

        // Windows are only meaningful once the importer has cut the clip to its motion; before
        // that (an import from an older importer version) binding waits for the reimport.
        public static bool Trimmed(string path, AnimationClip clip) =>
            !FbxCurveSpan.TryRead(path, out double start, out double end)
            || clip.length <= end - start + 0.05f;

        // Seconds of the original recording -> normalized time of the imported (trimmed) clip.
        public static float Normalized(AnimationClip clip, float seconds) =>
            clip.length > 0f ? Mathf.Clamp01((seconds - CalibrationFrame) / clip.length) : 0f;

        // By name; a file with a single clip gives that clip whatever it was called (an import
        // made before these names existed may have kept the original take name).
        public static AnimationClip FindClip(string path, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null) return null;
            AnimationClip[] clips = assets.OfType<AnimationClip>().Where(a => !a.name.StartsWith("__preview__")).ToArray();
            return clips.FirstOrDefault(a => a.name == clipName || a.name.EndsWith("|" + clipName))
                   ?? (clips.Length == 1 && !clipName.EndsWith(CharacterAnimationImporter.MirrorSuffix) ? clips[0] : null);
        }
    }
}
