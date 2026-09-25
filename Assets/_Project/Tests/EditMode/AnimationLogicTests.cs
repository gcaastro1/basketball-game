using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Presentation;

// Etapa 6: the pure side of animation -- which pose a player is in, what that pose does
// to the body, hand IK and model fitting.
public class AnimationLogicTests
{
    private static PlayerAnimInput Standing() => new PlayerAnimInput
    {
        Speed = 0f, MaxSpeed = 6f, Grounded = true, SincePass = 99f, SinceTeamScored = 99f
    };

    // ---------- state -> pose ----------

    [Test]
    public void Mapper_BallHandler_DribblesWhenMovingAndHoldsWhenStill()
    {
        var i = Standing();
        i.HasBall = true;
        Assert.AreEqual(AnimPose.HoldBall, AnimationStateMapper.Map(i).Pose);
        i.Speed = 3f;
        Assert.AreEqual(AnimPose.Dribble, AnimationStateMapper.Map(i).Pose);
        Assert.AreEqual(0.5f, AnimationStateMapper.Map(i).SpeedRatio, 1e-5f);
    }

    [Test]
    public void Mapper_ShotTypeWins_OverEverythingElse()
    {
        var i = Standing();
        i.HasBall = true;
        i.Grounded = false;
        i.Shooting = true;
        i.ShotType = ShotType.JumpShot;
        Assert.AreEqual(AnimPose.JumpShot, AnimationStateMapper.Map(i).Pose);
        i.ShotType = ShotType.FreeThrow;
        Assert.AreEqual(AnimPose.JumpShot, AnimationStateMapper.Map(i).Pose);
        i.ShotType = ShotType.Layup;
        Assert.AreEqual(AnimPose.Layup, AnimationStateMapper.Map(i).Pose);
        i.ShotType = ShotType.Dunk;
        Assert.AreEqual(AnimPose.Dunk, AnimationStateMapper.Map(i).Pose);
    }

    [Test]
    public void Mapper_Defender_StancesOnTheGroundAndBlocksInTheAir()
    {
        var i = Standing();
        i.Defending = true;
        Assert.AreEqual(AnimPose.Defense, AnimationStateMapper.Map(i).Pose);
        i.Grounded = false;
        Assert.AreEqual(AnimPose.Block, AnimationStateMapper.Map(i).Pose);
    }

    [Test]
    public void Mapper_PassAndCelebrate_AreShortLived()
    {
        var i = Standing();
        i.SincePass = 0.1f;
        Assert.AreEqual(AnimPose.Pass, AnimationStateMapper.Map(i).Pose);
        i.SincePass = 1f;
        Assert.AreEqual(AnimPose.Locomotion, AnimationStateMapper.Map(i).Pose);
        i.SinceTeamScored = 0.5f;
        Assert.AreEqual(AnimPose.Celebrate, AnimationStateMapper.Map(i).Pose);
        i.SinceTeamScored = 5f;
        Assert.AreEqual(AnimPose.Locomotion, AnimationStateMapper.Map(i).Pose);
    }

    // ---------- pose -> body ----------

    private static PoseChannels Pose(AnimPose pose, float speed = 0f, float stride = 0f, float t = 0f, bool grounded = true, float dribble = 0f) =>
        ProceduralPoseMath.Compute(new PoseParams
        {
            Pose = pose, SpeedRatio = speed, StridePhase = stride, ActionT = t, Grounded = grounded, DribblePhase = dribble
        });

    [Test]
    public void Running_SwingsLegsOppositeEachOther_AndArmsOppositeTheLegs()
    {
        PoseChannels c = Pose(AnimPose.Locomotion, speed: 1f, stride: Mathf.PI * 0.5f);
        Assert.Greater(c.LegSwingL, 0.3f);
        Assert.AreEqual(-c.LegSwingL, c.LegSwingR, 1e-5f);
        Assert.Less(c.ArmSwingL, 0f, "left arm swings back as the left leg goes forward");
        Assert.AreEqual(-c.ArmSwingL, c.ArmSwingR, 1e-5f);

        PoseChannels still = Pose(AnimPose.Locomotion, speed: 0f, stride: Mathf.PI * 0.5f);
        Assert.AreEqual(0f, still.LegSwingL, 1e-5f, "no stride when standing");
    }

    [Test]
    public void JumpShot_RaisesTheBallAndExtendsTheElbowsToRelease()
    {
        PoseChannels gather = Pose(AnimPose.JumpShot, t: 0f);
        PoseChannels release = Pose(AnimPose.JumpShot, t: 1f, grounded: false);
        PoseChannels run = Pose(AnimPose.Locomotion, speed: 1f);
        Assert.Greater(gather.ArmRaiseR, run.ArmRaiseR + 0.3f);
        Assert.Greater(release.ArmRaiseR, gather.ArmRaiseR);
        Assert.Less(release.ElbowBendR, gather.ElbowBendR, "elbow extends at release");
        Assert.Greater(gather.Crouch, release.Crouch, "gathers low, rises straight");
    }

    [Test]
    public void Defense_IsLowerThanRunning_AndBlockPutsBothHandsUp()
    {
        Assert.Greater(Pose(AnimPose.Defense).Crouch, Pose(AnimPose.Locomotion, speed: 1f).Crouch + 0.3f);
        PoseChannels block = Pose(AnimPose.Block, grounded: false);
        Assert.AreEqual(1f, block.ArmRaiseL, 1e-5f);
        Assert.AreEqual(1f, block.ArmRaiseR, 1e-5f);
    }

    [Test]
    public void Dribble_PumpsTheRightElbowWithTheBounce()
    {
        float top = Pose(AnimPose.Dribble, speed: 0.5f, dribble: Mathf.PI * 0.5f).ElbowBendR;
        float bottom = Pose(AnimPose.Dribble, speed: 0.5f, dribble: 0f).ElbowBendR;
        Assert.Greater(bottom - top, 0.3f);
    }

    [Test]
    public void EveryPose_StaysWithinChannelRange()
    {
        foreach (AnimPose pose in System.Enum.GetValues(typeof(AnimPose)))
        {
            for (float phase = 0f; phase < 7f; phase += 0.7f)
            {
                PoseChannels c = Pose(pose, speed: 1f, stride: phase, t: phase / 7f, grounded: phase < 3f, dribble: phase);
                foreach (float v in new[] { c.LegSwingL, c.LegSwingR, c.KneeBendL, c.KneeBendR, c.ArmSwingL, c.ArmSwingR,
                                            c.ArmRaiseL, c.ArmRaiseR, c.ElbowBendL, c.ElbowBendR, c.SpineLean, c.Crouch })
                {
                    Assert.That(v, Is.InRange(-1f, 1f), pose + " at phase " + phase);
                }
            }
        }
    }

    // ---------- hand IK ----------

    private static readonly Vector3 Shoulder = new Vector3(0f, 1.5f, 0f);
    private static readonly Vector3 Elbow = new Vector3(0.3f, 1.5f, 0f);
    private static readonly Vector3 Hand = new Vector3(0.55f, 1.5f, 0f);

    [Test]
    public void TwoBoneIK_ReachableTarget_HandArrivesAndBonesKeepTheirLength()
    {
        var target = new Vector3(0.2f, 1.2f, 0.3f);
        TwoBoneIK.Solve(Shoulder, Elbow, Hand, target, Shoulder + Vector3.down + Vector3.back, out Vector3 mid, out Vector3 end);

        Assert.Less(Vector3.Distance(end, target), 1e-3f);
        Assert.AreEqual(0.3f, Vector3.Distance(Shoulder, mid), 1e-3f);
        Assert.AreEqual(0.25f, Vector3.Distance(mid, end), 1e-3f);
    }

    [Test]
    public void TwoBoneIK_OutOfReach_StretchesTowardTheTarget()
    {
        var target = new Vector3(0f, 1.5f, 3f);
        TwoBoneIK.Solve(Shoulder, Elbow, Hand, target, Vector3.down, out Vector3 mid, out Vector3 end);

        Assert.AreEqual(0.55f, Vector3.Distance(Shoulder, end), 1e-3f, "fully extended");
        Assert.Greater(Vector3.Dot((end - Shoulder).normalized, (target - Shoulder).normalized), 0.999f);
    }

    [Test]
    public void TwoBoneIK_ElbowBendsTowardThePole()
    {
        var target = new Vector3(0.35f, 1.5f, 0f);
        TwoBoneIK.Solve(Shoulder, Elbow, Hand, target, new Vector3(0.2f, 0.5f, 0f), out Vector3 mid, out _);
        Assert.Less(mid.y, Shoulder.y - 0.05f, "elbow drops toward the pole below");
    }

    // ---------- model fitting ----------

    [Test]
    public void ModelFitting_ScalesToHeightAndStandsOnTheFeet()
    {
        ModelFitting.Fit(-0.1f, 0.9f, 1.9f, out float scale, out float yOffset);
        Assert.AreEqual(1.9f, scale, 1e-5f);
        Assert.AreEqual(0.19f, yOffset, 1e-5f, "lowest point lands on the floor");
    }
}
