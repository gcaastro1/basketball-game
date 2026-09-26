using NUnit.Framework;
using Basket.EditorTools;
using Basket.Presentation;

// Real clips: how locomotion clips blend by speed, and which imported clips loop.
public class AnimationClipLogicTests
{
    private const float Walk = 1.5f, Run = 4.5f;

    private static LocomotionMix Blend(float speed, float forward, float right = 0f, float yawRate = 0f,
        bool walk = true, bool back = true, bool sides = true, bool turns = true)
    {
        var i = new LocomotionInput
        {
            Speed = speed, Forward = forward, Right = right, YawRate = yawRate, MoveSpeed = Walk, RunSpeed = Run,
            HasForward = walk, HasBackward = back, HasLeft = sides, HasRight = sides, HasRun = true,
            HasTurnLeft = turns, HasTurnRight = turns,
        };
        return LocomotionBlend.Compute(i);
    }

    [Test]
    public void Standing_IsAllIdle()
    {
        var w = Blend(0f, 0f);
        Assert.AreEqual(1f, w.Idle, 1e-4f);
        Assert.AreEqual(1f, w.Sum, 1e-4f);
    }

    [Test]
    public void SpeedUp_GoesIdleToWalkToRun()
    {
        var slow = Blend(0.75f, 0.75f);
        Assert.AreEqual(0.5f, slow.Idle, 1e-4f);
        Assert.AreEqual(0.5f, slow.Forward, 1e-4f);

        var mid = Blend(3f, 3f);
        Assert.AreEqual(0f, mid.Idle, 1e-4f);
        Assert.AreEqual(0.5f, mid.Run, 1e-4f);
        Assert.AreEqual(0.5f, mid.Forward, 1e-4f);

        var fast = Blend(6f, 6f);
        Assert.AreEqual(1f, fast.Run, 1e-4f);
        Assert.AreEqual(1f, fast.Sum, 1e-4f);
    }

    [Test]
    public void WithoutWalkClip_ForwardMovementRuns()
    {
        var w = Blend(0.75f, 0.75f, walk: false);
        Assert.AreEqual(0f, w.Forward);
        Assert.AreEqual(0.5f, w.Run, 1e-4f);
        Assert.AreEqual(0.5f, w.Idle, 1e-4f);
    }

    [Test]
    public void Direction_PicksBackwardAndSideClips()
    {
        var back = Blend(1.5f, -1.5f);
        Assert.AreEqual(1f, back.Backward, 1e-4f);
        var right = Blend(1.5f, 0f, right: 1.5f);
        Assert.AreEqual(1f, right.Right, 1e-4f);
        var left = Blend(1.5f, 0f, right: -1.5f);
        Assert.AreEqual(1f, left.Left, 1e-4f);
        var diagonal = Blend(1.5f, 1.06f, right: 1.06f);
        Assert.AreEqual(0.5f, diagonal.Forward, 1e-2f);
        Assert.AreEqual(0.5f, diagonal.Right, 1e-2f);
        Assert.AreEqual(1f, diagonal.Sum, 1e-4f);
    }

    [Test]
    public void MissingClips_HandTheirShareToTheNearestOne()
    {
        var noSides = Blend(1.5f, 0f, right: 1.5f, sides: false);
        Assert.AreEqual(1f, noSides.Forward, 1e-4f);
        var noBack = Blend(1.5f, -1.5f, back: false);
        Assert.AreEqual(1f, noBack.Forward, 1e-4f);
        Assert.AreEqual(1f, noBack.Sum, 1e-4f);
    }

    [Test]
    public void Running_BendsIntoTheTurnClips()
    {
        var right = Blend(6f, 6f, yawRate: 90f);
        Assert.AreEqual(0.5f, right.TurnRight, 1e-4f);
        Assert.AreEqual(0.5f, right.Run, 1e-4f);
        var hardLeft = Blend(6f, 6f, yawRate: -400f);
        Assert.AreEqual(1f, hardLeft.TurnLeft, 1e-4f);
        Assert.AreEqual(1f, Blend(6f, 6f, yawRate: 90f, turns: false).Run, 1e-4f);
    }

    [Test]
    public void Playback_FollowsTheRealSpeed_WithinLimits()
    {
        Assert.AreEqual(1f, Blend(Run, Run).RunRate, 1e-4f, "at the clip's own speed");
        Assert.AreEqual(5f / Run, Blend(5f, 5f).RunRate, 1e-4f, "a bit faster when faster");
        Assert.AreEqual(LocomotionBlend.MaxRate, Blend(9f, 9f).RunRate, 1e-4f, "a sprint is not fast-forwarded");
        Assert.AreEqual(LocomotionBlend.MinRate, Blend(0.1f, 0.1f).MoveRate, 1e-4f);
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

    // Mocap takes declare spans far longer than their motion; the clip covers the motion only.
    [Test]
    public void Importer_FrameRange_CoversTheMotion_WithoutTheCalibrationFrame()
    {
        (float first, float last) = CharacterAnimationImporter.FrameRange(0.0, 0.9333, skipCalibration: true);
        Assert.AreEqual(1f, first);
        Assert.AreEqual(28f, last);
        (first, last) = CharacterAnimationImporter.FrameRange(0.0, 6.6, skipCalibration: false);
        Assert.AreEqual(0f, first);
        Assert.AreEqual(198f, last);
        Assert.IsTrue(CharacterAnimationImporter.IsMocap("CharacterArmature|102_05_remap"));
        Assert.IsFalse(CharacterAnimationImporter.IsMocap("mixamo.com"));
    }

    [Test]
    public void CurveSpan_OfTheShippedMocapClips()
    {
        const string folder = "Assets/TripoModels/anime_character_3d_model/Animations/Basquete/";
        if (!System.IO.File.Exists(folder + "RunningStraight_102_05.fbx")) Assert.Ignore("mocap clips not in this checkout");
        Assert.IsTrue(FbxCurveSpan.TryRead(folder + "RunningStraight_102_05.fbx", out double start, out double end));
        Assert.AreEqual(0.0, start, 1e-3);
        Assert.AreEqual(0.933, end, 0.01, "not the 88.5 s the take declares");
        Assert.IsTrue(FbxCurveSpan.TryRead(folder + "Basketball_Jump_Shot_124_05.fbx", out start, out end));
        Assert.AreEqual(6.6, end, 0.01);
        Assert.IsFalse(FbxCurveSpan.TryRead(new byte[] { 1, 2, 3 }, out _, out _));
    }

    [Test]
    public void OneShotAction_PlaysThroughItsWindow_AndHolds()
    {
        Assert.AreEqual(3.0f, ActionClipTiming.Play(0.5f, 0.6f, 0f, 6f), 1e-4f, "starts at the window start");
        Assert.AreEqual(3.3f, ActionClipTiming.Play(0.5f, 0.6f, 0.3f, 6f), 1e-4f);
        Assert.AreEqual(3.6f, ActionClipTiming.Play(0.5f, 0.6f, 5f, 6f), 1e-4f, "holds the window end");
    }
}
