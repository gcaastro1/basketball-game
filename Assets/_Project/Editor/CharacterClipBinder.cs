using System.Linq;
using UnityEditor;
using UnityEngine;
using Basket.Characters;

namespace Basket.EditorTools
{
    // Fills the default character's empty clip slots with the animations shipped under
    // Assets/TripoModels/anime_character_3d_model/Animations (Mixamo, Universal Animation
    // Library (CC0) and the basketball mocap set in Animations/Basquete). Clip references inside an FBX carry Unity-generated ids, so they are
    // bound here, in the editor, instead of by hand in the asset file.
    //
    // Runs after those files import and once per editor load; only empty slots are filled,
    // so anything set in the Inspector wins. Menu: Basket -> Bind Character Animations.
    public sealed class CharacterClipBinder : AssetPostprocessor
    {
        public const string VisualPath = "Assets/_Project/Data/Characters/DefaultCharacterVisual.asset";
        private const string Folder = TripoHumanoidImporter.TripoFolder + "anime_character_3d_model/Animations/";
        private const string Ual = Folder + "Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
        private const string Basket = Folder + "Basquete/";

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
            int bound = 0;
            bound += Fill(ref c.idle, Folder + "Idle.fbx", "Idle");
            bound += Fill(ref c.walk, Folder + "Walking.fbx", "Walking");
            bound += Fill(ref c.run, Folder + "Running.fbx", "Running");
            bound += Fill(ref c.runBackward, Folder + "Running Backward.fbx", "Running Backward");
            // Moving-dribble mocap: it plays on the upper body while the legs run.
            bound += Fill(ref c.dribble, Basket + "basketball_forward_dribble_06_02.fbx", "basketball_forward_dribble_06_02");
            bound += Fill(ref c.dribble, Folder + "Dribble.fbx", "Dribble");
            bound += Fill(ref c.defense, Folder + "Offensive Idle.fbx", "Offensive Idle");
            bound += Fill(ref c.airborne, Ual, "Jump_Loop");
            // Shot windows measured on the clips' hip height: the crouch before the jump
            // (start) and the top of the jump, where the ball leaves (release).
            bound += Fill(ref c.jumpShot, Basket + "Basketball_Jump_Shot_124_05.fbx", "Basketball_Jump_Shot_124_05",
                ref c.jumpShotWindow, new ClipWindow(0.464f, 0.526f));
            bound += Fill(ref c.layup, Basket + "Basketball_Lay_Up_124_06.fbx", "Basketball_Lay_Up_124_06",
                ref c.layupWindow, new ClipWindow(0.503f, 0.595f));
            bound += Fill(ref c.freeThrow, Basket + "Basketball_Free_Throw_124_04.fbx", "Basketball_Free_Throw_124_04",
                ref c.freeThrowWindow, new ClipWindow(0.824f, 0.905f));
            bound += Fill(ref c.defenseMove, Basket + "DefensiveMoveSideToSide_102_27.fbx", "DefensiveMoveSideToSide_102_27");
            if (bound > 0)
            {
                EditorUtility.SetDirty(visual);
                AssetDatabase.SaveAssets();
            }
            return bound;
        }

        private static int Fill(ref AnimationClip slot, string path, string clipName, ref ClipWindow window, ClipWindow measured)
        {
            int filled = Fill(ref slot, path, clipName);
            if (filled > 0) window = measured;
            return filled;
        }

        private static int Fill(ref AnimationClip slot, string path, string clipName)
        {
            if (slot != null) return 0;
            AnimationClip clip = FindClip(path, clipName);
            if (clip == null) return 0;
            slot = clip;
            return 1;
        }

        // By name; a file with a single clip gives that clip whatever it was called (an import
        // made before these names existed may have kept the original take name).
        public static AnimationClip FindClip(string path, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null) return null;
            AnimationClip[] clips = assets.OfType<AnimationClip>().Where(a => !a.name.StartsWith("__preview__")).ToArray();
            return clips.FirstOrDefault(a => a.name == clipName || a.name.EndsWith("|" + clipName))
                   ?? (clips.Length == 1 ? clips[0] : null);
        }
    }
}
