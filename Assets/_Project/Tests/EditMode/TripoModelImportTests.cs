using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Etapa 6: the Tripo character must import as a valid Humanoid (shared animations,
// HumanPoseHandler and hand IK depend on it). Runs against the real importer in CI.
public class TripoModelImportTests
{
    private const string ModelPath = "Assets/TripoModels/anime_character_3d_model/anime_character_3d_model.fbx";

    [Test]
    public void TripoModel_ImportsAsHumanoidWithAValidAvatar()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        Assert.IsNotNull(importer, "model importer at " + ModelPath);
        Assert.AreEqual(ModelImporterAnimationType.Human, importer.animationType);

        Avatar avatar = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (o is Avatar a) avatar = a;
        }
        Assert.IsNotNull(avatar, "an avatar is generated from the model");
        Assert.IsTrue(avatar.isValid, "avatar is valid");
        Assert.IsTrue(avatar.isHuman, "avatar is humanoid");
    }

    [Test]
    public void TripoModel_MapsTheBonesGameplayAnimationUses()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        Assert.IsNotNull(prefab);
        GameObject instance = Object.Instantiate(prefab);
        try
        {
            var animator = instance.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator, "the humanoid import adds an Animator");
            foreach (HumanBodyBones bone in new[]
                     {
                         HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Head,
                         HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                         HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                         HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                         HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
                     })
            {
                Assert.IsNotNull(animator.GetBoneTransform(bone), bone + " is mapped");
            }
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }
}
