using UnityEditor;

namespace Basket.EditorTools
{
    // Character models brought in by the Tripo3D Bridge (Assets/TripoModels) come in as
    // Generic rigs. Gameplay animation (Etapa 6) needs a Humanoid avatar -- shared
    // animations, HumanPoseHandler and hand IK are all humanoid-only -- so every model
    // imported there is set to Humanoid with an avatar created from its own skeleton.
    // Tripo's bone names (Hips, Spine, Chest, Left_UpperArm, ...) are recognised by
    // Unity's automatic human mapping.
    public sealed class TripoHumanoidImporter : AssetPostprocessor
    {
        public const string TripoFolder = "Assets/TripoModels/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(TripoFolder)) return;
            var importer = (ModelImporter)assetImporter;
            if (importer.animationType == ModelImporterAnimationType.Human) return;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        }
    }
}
