using System.Linq;
using UnityEditor;
using UnityEngine;
using Basket.Characters;

namespace Basket.EditorTools
{
    // One-off wiring of the Asset Store basketball mocap pack + the Mixamo/UAL locomotion set
    // into DefaultCharacterVisual (D-022). Provisional mapping -- action clip windows (gather..
    // release) default to 0..0.5 since the exact release frame needs visual confirmation in the
    // Animation window; guard/pass/block/dunk/celebrate are left unset (procedural fallback) since
    // the pack's clip names don't disambiguate direction/semantics confidently from names alone.
    public static class WireCharacterAnimations
    {
        private const string VisualAssetPath = "Assets/_Project/Data/Characters/DefaultCharacterVisual.asset";
        private const string BasqueteDir = "Assets/TripoModels/anime_character_3d_model/Animations/Basquete";
        private const string AnimDir = "Assets/TripoModels/anime_character_3d_model/Animations";

        [MenuItem("Basket/Debug/Wire Default Character Animations")]
        public static void Run()
        {
            var visual = AssetDatabase.LoadAssetAtPath<CharacterVisualDefinition>(VisualAssetPath);
            if (visual == null)
            {
                Debug.LogError($"Could not load {VisualAssetPath}");
                return;
            }

            var c = visual.clips;

            c.free.idle = new LoopClip(Clip(AnimDir, "Idle.fbx", "Idle"));
            c.free.forward = new LoopClip(Clip(AnimDir, "Walking.fbx", "Walking"));
            c.free.run = new LoopClip(Clip(AnimDir, "Running.fbx", "Running"));

            c.withBall.idle = new LoopClip(Clip(AnimDir, "Offensive Idle.fbx", "Offensive Idle"));
            c.withBall.forward = new LoopClip(Clip(BasqueteDir, "basketball_forward_dribble_06_02.fbx", "basketball_forward_dribble_06_02"));
            c.withBall.backward = new LoopClip(Clip(BasqueteDir, "basketball_backward_dribble_06_06.fbx", "basketball_backward_dribble_06_06"));
            c.withBall.left = new LoopClip(Clip(BasqueteDir, "basketball_sideways_dribble_06_08.fbx", "basketball_sideways_dribble_06_08"));
            c.withBall.right = new LoopClip(Clip(BasqueteDir, "basketball_sideways_dribble_06_08.fbx", "basketball_sideways_dribble_06_08_Mirror"));
            c.withBall.run = new LoopClip(Clip(BasqueteDir, "basketball_forward_dribble_06_04.fbx", "basketball_forward_dribble_06_04"));
            c.withBall.runTurnLeft = new LoopClip(Clip(BasqueteDir, "basketball_forward_dribble_90_degree_left_turns_06_10.fbx", "basketball_forward_dribble_90_degree_left_turns_06_10"));
            c.withBall.runTurnRight = new LoopClip(Clip(BasqueteDir, "basketball_forward_dribble_90_degree_right_turns_06_11.fbx", "basketball_forward_dribble_90_degree_right_turns_06_11"));

            c.dribbleUpperBody = new LoopClip(Clip(AnimDir, "Dribble.fbx", "Dribble"));
            c.holdBall = new LoopClip(Clip(AnimDir, "Offensive Idle.fbx", "Offensive Idle"));

            c.jumpShots = new[] { new ActionClip(Clip(BasqueteDir, "Basketball_Jump_Shot_124_05.fbx", "Basketball_Jump_Shot_124_05"), ClipWindow.Default) };
            c.jumpShotsMoving = new[] { new ActionClip(Clip(BasqueteDir, "basketball_dribble_shoot_06_15.fbx", "basketball_dribble_shoot_06_15"), ClipWindow.Default) };
            c.layup = new ActionClip(Clip(BasqueteDir, "Basketball_Lay_Up_124_06.fbx", "Basketball_Lay_Up_124_06"), ClipWindow.Default);
            c.freeThrow = new ActionClip(Clip(BasqueteDir, "Basketball_Free_Throw_124_04.fbx", "Basketball_Free_Throw_124_04"), ClipWindow.Default);

            EditorUtility.SetDirty(visual);
            AssetDatabase.SaveAssets();
            Debug.Log("Wired DefaultCharacterVisual clips. Guard/pass/block/dunk/celebrate left unset (procedural fallback) -- verify shot/layup/free-throw release timing in the Animation window and adjust window.start/release per clip.");
        }

        private static AnimationClip Clip(string dir, string fileName, string clipName)
        {
            string path = $"{dir}/{fileName}";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == clipName);
            if (clip == null) Debug.LogError($"Clip '{clipName}' not found in {path}");
            return clip;
        }
    }
}
