using NUnit.Framework;
using Basket.EditorTools;
using Basket.Presentation;

// Real clips: how locomotion clips blend by speed, and which imported clips loop.
public class AnimationClipLogicTests
{
    private const float Walk = 1.5f, Run = 4.5f;

    private static LocomotionWeights Blend(float speed, float forward, bool walk = true, bool back = true) =>
        LocomotionBlend.Compute(speed, forward, Walk, Run, walk, back);

    private static float Sum(LocomotionWeights w) => w.Idle + w.Walk + w.Run + w.Back;

    [Test]
    public void Standing_IsAllIdle()
    {
        var w = Blend(0f, 0f);
        Assert.AreEqual(1f, w.Idle, 1e-4f);
        Assert.AreEqual(1f, Sum(w), 1e-4f);
    }

    [Test]
    public void SpeedUp_GoesIdleToWalkToRun()
    {
        var slow = Blend(0.75f, 0.75f);
        Assert.AreEqual(0.5f, slow.Idle, 1e-4f);
        Assert.AreEqual(0.5f, slow.Walk, 1e-4f);

        var mid = Blend(3f, 3f);
        Assert.AreEqual(0f, mid.Idle, 1e-4f);
        Assert.AreEqual(0.5f, mid.Run, 1e-4f);
        Assert.AreEqual(0.5f, mid.Walk, 1e-4f);

        var fast = Blend(6f, 6f);
        Assert.AreEqual(1f, fast.Run, 1e-4f);
        Assert.AreEqual(1f, Sum(fast), 1e-4f);
    }

    [Test]
    public void WithoutWalkClip_IdleBlendsStraightToRun()
    {
        var w = Blend(2.25f, 2.25f, walk: false);
        Assert.AreEqual(0f, w.Walk);
        Assert.AreEqual(0.5f, w.Run, 1e-4f);
        Assert.AreEqual(0.5f, w.Idle, 1e-4f);
    }

    [Test]
    public void MovingBackward_UsesTheBackpedal_SidewaysDoesNot()
    {
        var back = Blend(3f, -3f);
        Assert.AreEqual(1f, back.Back, 1e-4f);
        Assert.AreEqual(0f, back.Run + back.Walk, 1e-4f);

        var side = Blend(3f, 0f);
        Assert.AreEqual(0f, side.Back, 1e-4f);
        Assert.AreEqual(1f, Sum(side), 1e-4f);

        var noClip = Blend(3f, -3f, back: false);
        Assert.AreEqual(0f, noClip.Back);
        Assert.AreEqual(1f, Sum(noClip), 1e-4f);
    }

    [Test]
    public void Playback_FollowsTheRealSpeed_WithinLimits()
    {
        Assert.AreEqual(1f, Blend(Run, Run).RunRate, 1e-4f, "at the clip's own speed");
        Assert.AreEqual(6f / Run, Blend(6f, 6f).RunRate, 1e-4f, "faster feet when faster");
        Assert.AreEqual(LocomotionBlend.MaxRate, Blend(20f, 20f).RunRate, 1e-4f);
        Assert.AreEqual(LocomotionBlend.MinRate, Blend(0.1f, 0.1f).WalkRate, 1e-4f);
    }

    [Test]
    public void Importer_NamesAndLoops()
    {
        Assert.AreEqual("Running", CharacterAnimationImporter.ClipName("mixamo.com", "Running", 1));
        Assert.AreEqual("Jump_Loop", CharacterAnimationImporter.ClipName("Armature|Jump_Loop", "UAL1_Standard", 45));

        foreach (string loop in new[] { "Idle", "Offensive Idle", "Walking", "Running", "Running Backward", "Dribble", "Jump_Loop", "Sprint_Loop" })
            Assert.IsTrue(CharacterAnimationImporter.IsLoop(loop), loop);
        foreach (string once in new[] { "Start Walking", "Jump_Start", "Jump_Land", "Sitting_Enter", "Death01", "Punch_Jab" })
            Assert.IsFalse(CharacterAnimationImporter.IsLoop(once), once);
    }

    [Test]
    public void Importer_HandlesOnlyCharacterAnimationFolders()
    {
        Assert.IsTrue(CharacterAnimationImporter.Handles("Assets/TripoModels/anime_character_3d_model/Animations/Running.fbx"));
        Assert.IsFalse(CharacterAnimationImporter.Handles("Assets/TripoModels/anime_character_3d_model/anime_character_3d_model.fbx"));
        Assert.IsFalse(CharacterAnimationImporter.Handles("Assets/Other/Animations/Running.fbx"));
    }
}
