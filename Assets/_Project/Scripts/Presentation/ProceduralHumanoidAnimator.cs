using System;
using UnityEngine;

namespace Basket.Presentation
{
    // Drives a humanoid through Unity's muscle space (HumanPoseHandler): the same code
    // animates any humanoid avatar, whatever its bone axes. Alone until real clips exist, then
    // only for the poses that still have no clip, blended over them.
    //
    // Muscle values are Unity's normalized humanoid space (-1..1). The table below is the
    // only place that knows them; tune it here if a pose reads wrong on a model.
    public sealed class ProceduralHumanoidAnimator : IDisposable
    {
        // Reference values (humanoid muscle space) for a relaxed standing body.
        private const float LegStraight = 0.6f;       // Upper Leg Front-Back, leg under the hip
        private const float LegSwingRange = 0.5f;     // +/- stride
        private const float HipFlexPerCrouch = 0.35f;
        private const float KneeStraight = 1f;        // Lower Leg Stretch
        private const float KneeBendRange = 1.6f;     // bend 1 -> -0.6
        private const float ArmDown = -0.65f;         // Arm Down-Up, arm along the body
        private const float ArmHorizontal = 0.4f;
        private const float ArmOverhead = 1f;
        private const float ArmFrontNeutral = 0.3f;   // Arm Front-Back
        private const float ArmSwingRange = 0.5f;
        private const float ElbowStraight = 1f;       // Forearm Stretch
        private const float ElbowBendRange = 1.3f;    // bend 1 -> -0.3
        private const float SpineLeanRange = 0.5f;    // Spine/Chest Front-Back
        private const float CrouchDrop = 0.18f;       // body lowered per unit crouch (humanoid units)

        private readonly HumanPoseHandler handler;
        private HumanPose pose;
        private readonly Vector3 restBodyPosition;
        private readonly Quaternion restBodyRotation;

        private readonly int legL, legR, kneeL, kneeR, armL, armR, armFbL, armFbR, elbowL, elbowR, spine, chest;

        public ProceduralHumanoidAnimator(Animator animator)
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                throw new ArgumentException("Procedural animation needs a humanoid Animator.");
            handler = new HumanPoseHandler(animator.avatar, animator.transform);
            pose = new HumanPose();
            handler.GetHumanPose(ref pose);
            restBodyPosition = pose.bodyPosition;
            restBodyRotation = pose.bodyRotation;

            legL = Muscle("Left Upper Leg Front-Back");
            legR = Muscle("Right Upper Leg Front-Back");
            kneeL = Muscle("Left Lower Leg Stretch");
            kneeR = Muscle("Right Lower Leg Stretch");
            armL = Muscle("Left Arm Down-Up");
            armR = Muscle("Right Arm Down-Up");
            armFbL = Muscle("Left Arm Front-Back");
            armFbR = Muscle("Right Arm Front-Back");
            elbowL = Muscle("Left Forearm Stretch");
            elbowR = Muscle("Right Forearm Stretch");
            spine = Muscle("Spine Front-Back");
            chest = Muscle("Chest Front-Back");
        }

        // weight 1: the procedural pose alone (no clips). Below 1 it is blended over the pose
        // the clips already put the body in (actions without a clip, e.g. a jump shot).
        public void Apply(in PoseChannels c, float weight = 1f)
        {
            if (weight <= 0f) return;
            handler.GetHumanPose(ref pose);
            float[] m = pose.muscles;
            Set(m, legL, LegStraight + LegSwingRange * c.LegSwingL + HipFlexPerCrouch * c.Crouch, weight);
            Set(m, legR, LegStraight + LegSwingRange * c.LegSwingR + HipFlexPerCrouch * c.Crouch, weight);
            Set(m, kneeL, KneeStraight - KneeBendRange * c.KneeBendL, weight);
            Set(m, kneeR, KneeStraight - KneeBendRange * c.KneeBendR, weight);
            Set(m, armL, ArmRaise(c.ArmRaiseL), weight);
            Set(m, armR, ArmRaise(c.ArmRaiseR), weight);
            Set(m, armFbL, ArmFrontNeutral + ArmSwingRange * c.ArmSwingL, weight);
            Set(m, armFbR, ArmFrontNeutral + ArmSwingRange * c.ArmSwingR, weight);
            Set(m, elbowL, ElbowStraight - ElbowBendRange * c.ElbowBendL, weight);
            Set(m, elbowR, ElbowStraight - ElbowBendRange * c.ElbowBendR, weight);
            Set(m, spine, SpineLeanRange * c.SpineLean, weight);
            Set(m, chest, SpineLeanRange * 0.5f * c.SpineLean, weight);
            Vector3 body = restBodyPosition + Vector3.down * (CrouchDrop * c.Crouch);
            pose.bodyPosition = weight >= 1f ? body : Vector3.Lerp(pose.bodyPosition, body, weight);
            pose.bodyRotation = weight >= 1f ? restBodyRotation : Quaternion.Slerp(pose.bodyRotation, restBodyRotation, weight);
            handler.SetHumanPose(ref pose);
        }

        // 0 = along the body, 0.5 = horizontal, 1 = overhead.
        private static float ArmRaise(float raise) => raise <= 0.5f
            ? Mathf.Lerp(ArmDown, ArmHorizontal, raise * 2f)
            : Mathf.Lerp(ArmHorizontal, ArmOverhead, (raise - 0.5f) * 2f);

        private static void Set(float[] muscles, int index, float value, float weight)
        {
            if (index < 0 || index >= muscles.Length) return;
            float target = Mathf.Clamp(value, -1f, 1f);
            muscles[index] = weight >= 1f ? target : Mathf.Lerp(muscles[index], target, weight);
        }

        private static int Muscle(string name) => Array.IndexOf(HumanTrait.MuscleName, name);

        public void Dispose() => handler.Dispose();
    }
}
