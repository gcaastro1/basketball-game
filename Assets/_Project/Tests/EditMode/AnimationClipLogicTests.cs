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
        Assert.AreEqual(5f / Run, Blend(5f, 5f).RunRate, 1e-4f, "a bit faster when faster");
        Assert.AreEqual(LocomotionBlend.MaxRate, Blend(9f, 9f).RunRate, 1e-4f, "a sprint is not fast-forwarded");
        Assert.AreEqual(LocomotionBlend.MaxRate, Blend(20f, 20f).RunRate, 1e-4f);
        Assert.AreEqual(LocomotionBlend.MinRate, Blend(0.1f, 0.1f).WalkRate, 1e-4f);
    }

    [Test]
    public void Importer_NamesAndLoops()
    {
        Assert.AreEqual("Running", CharacterAnimationImporter.ClipName("mixamo.com", "Running", 1));
        Assert.AreEqual("Basketball_Jump_Shot_124_05",
            CharacterAnimationImporter.ClipName("CharacterArmature|124_05_remap", "Basketball_Jump_Shot_124_05", 1));
        Assert.AreEqual("Jump_Loop", CharacterAnimationImporter.ClipName("Armature|Jump_Loop", "UAL1_Standard", 45));

        foreach (string loop in new[] { "Idle", "Offensive Idle", "Walking", "Running", "Running Backward", "Dribble", "Jump_Loop", "Sprint_Loop",
                     "basketball_forward_dribble_06_02", "RunningStraight_102_05", "DefensiveMoveSideToSide_102_27", "basketball_backward_dribble_06_06" })
            Assert.IsTrue(CharacterAnimationImporter.IsLoop(loop), loop);
        foreach (string once in new[] { "Start Walking", "Jump_Start", "Jump_Land", "Sitting_Enter", "Death01", "Punch_Jab",
                     "Basketball_Jump_Shot_124_05", "Basketball_Free_Throw_124_04", "Basketball_Lay_Up_124_06", "basketball_dribble_shoot_06_15",
                     "basketball_forward_dribble_90_degree_left_turns_06_10", "DefensiveLeftStopToStop_102_25", "FakeShotBreakLeft_102_21",
                     "OffensiveMoveSpinLeft_102_11" })
            Assert.IsFalse(CharacterAnimationImporter.IsLoop(once), once);
    }

    [Test]
    public void Importer_HandlesOnlyCharacterAnimationFolders()
    {
        Assert.IsTrue(CharacterAnimationImporter.Handles("Assets/TripoModels/anime_character_3d_model/Animations/Running.fbx"));
        Assert.IsFalse(CharacterAnimationImporter.Handles("Assets/TripoModels/anime_character_3d_model/anime_character_3d_model.fbx"));
        Assert.IsFalse(CharacterAnimationImporter.Handles("Assets/Other/Animations/Running.fbx"));
    }

    [Test]
    public void ShotClip_FollowsTheGameplayShot_UpToItsRelease()
    {
        // 6 s mocap: gather at 0.45, release at 0.55 -> 2.7 s .. 3.3 s.
        Assert.AreEqual(2.7f, ActionClipTiming.TimeFor(0f, 0.45f, 0.55f, 6f), 1e-4f, "starts at the gather, skipping the lead-in");
        Assert.AreEqual(3.0f, ActionClipTiming.TimeFor(0.5f, 0.45f, 0.55f, 6f), 1e-4f, "half way to the release");
        Assert.AreEqual(3.3f, ActionClipTiming.TimeFor(1f, 0.45f, 0.55f, 6f), 1e-4f, "release on the gameplay release");
        Assert.AreEqual(1.5f, ActionClipTiming.TimeFor(1f, 0.5f, 0.2f, 3f), 1e-4f, "release before start: holds at start");
        Assert.IsTrue(ActionClipTiming.IsDriven(0.3f));
        Assert.IsFalse(ActionClipTiming.IsDriven(1f), "after the release the clip plays on");
        Assert.IsFalse(ActionClipTiming.IsDriven(-1f), "not shooting");
    }

    // FBX GlobalSettings "TimeMode" property as the binary format stores it.
    private static byte[] FbxWithTimeMode(int mode)
    {
        var bytes = new System.Collections.Generic.List<byte> { 1, 2, 3 };
        foreach (string s in new[] { "TimeMode", "enum", "", "" })
        {
            bytes.Add((byte)'S');
            bytes.AddRange(System.BitConverter.GetBytes(s.Length));
            foreach (char c in s) bytes.Add((byte)c);
        }
        bytes.Add((byte)'I');
        bytes.AddRange(System.BitConverter.GetBytes(mode));
        bytes.AddRange(new byte[] { 9, 9 });
        return bytes.ToArray();
    }

    [Test]
    public void FbxFixer_DropFrame30_Becomes30Fps_OtherModesUntouched()
    {
        byte[] drop = FbxWithTimeMode(FbxFrameRateFixer.Frames30Drop);
        int length = drop.Length;
        Assert.IsTrue(FbxFrameRateFixer.TryFix(drop));
        Assert.AreEqual(length, drop.Length, "same size: only the value changes");
        CollectionAssert.AreEqual(FbxWithTimeMode(FbxFrameRateFixer.Frames30), drop);
        Assert.IsFalse(FbxFrameRateFixer.TryFix(drop), "already fixed");

        byte[] mixamo = FbxWithTimeMode(3);
        Assert.IsFalse(FbxFrameRateFixer.TryFix(mixamo));
        CollectionAssert.AreEqual(FbxWithTimeMode(3), mixamo);
        Assert.IsFalse(FbxFrameRateFixer.TryFix(new byte[] { 1, 2, 3 }));
    }
}
