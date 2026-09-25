using UnityEngine;

namespace Basket.Presentation
{
    // A body pose in rig-independent terms. Leg/arm swing: -1 back .. +1 forward.
    // Bends, raises, lean and crouch: 0 neutral .. 1 fully. ProceduralHumanoidAnimator
    // turns these into humanoid muscles, so the same pose drives any humanoid model.
    public struct PoseChannels
    {
        public float LegSwingL, LegSwingR;
        public float KneeBendL, KneeBendR;
        public float ArmSwingL, ArmSwingR;
        public float ArmRaiseL, ArmRaiseR;
        public float ElbowBendL, ElbowBendR;
        public float SpineLean;
        public float Crouch;
    }

    public struct PoseParams
    {
        public AnimPose Pose;
        public float SpeedRatio;
        public bool Grounded;
        // Locomotion cycle (radians, advances with distance covered).
        public float StridePhase;
        // Dribble bounce phase (radians).
        public float DribblePhase;
        // 0..1 progress through a one-shot action (shot rise, pass push, ...).
        public float ActionT;
    }

    // Placeholder animation until real clips exist (CharacterVisualDefinition clips): the
    // body reads what the player is doing -- running, dribbling, a jump shot extending to
    // release -- from gameplay state alone.
    public static class ProceduralPoseMath
    {
        public static PoseChannels Compute(in PoseParams p)
        {
            PoseChannels c = Locomotion(p);
            float t = Clamp01(p.ActionT);
            switch (p.Pose)
            {
                case AnimPose.Dribble:
                    // Right hand pumps the ball; body a bit lower and forward.
                    float bounce = Mathf.Abs(Mathf.Sin(p.DribblePhase));
                    c.ArmSwingR = 0.25f;
                    c.ArmRaiseR = 0.15f;
                    c.ElbowBendR = 0.25f + 0.45f * (1f - bounce);
                    c.Crouch = Max(c.Crouch, 0.2f);
                    c.SpineLean = Max(c.SpineLean, 0.25f);
                    break;
                case AnimPose.HoldBall:
                    // Ball at the chest, both hands on it.
                    SetArms(ref c, raise: 0.35f, elbow: 0.65f, swing: 0.6f);
                    c.Crouch = Max(c.Crouch, 0.1f);
                    break;
                case AnimPose.Defense:
                    // Low stance, arms out and active.
                    SetArms(ref c, raise: 0.45f, elbow: 0.2f, swing: 0.15f);
                    c.Crouch = 0.5f;
                    c.SpineLean = 0.25f;
                    c.KneeBendL = Max(c.KneeBendL, 0.45f);
                    c.KneeBendR = Max(c.KneeBendR, 0.45f);
                    break;
                case AnimPose.JumpShot:
                    // Gather low, rise with the ball overhead, extend the elbows at release.
                    SetArms(ref c, raise: Lerp(0.55f, 1f, t), elbow: Lerp(0.85f, 0.1f, t), swing: 0.5f);
                    c.ArmRaiseL = Lerp(0.5f, 0.85f, t);
                    c.Crouch = p.Grounded ? 0.45f : 0.05f;
                    Stand(ref c);
                    break;
                case AnimPose.Layup:
                    // Shooting hand up, opposite knee driving up.
                    c.ArmRaiseR = 1f;
                    c.ElbowBendR = Lerp(0.5f, 0.05f, t);
                    c.ArmRaiseL = 0.4f;
                    c.ElbowBendL = 0.4f;
                    c.LegSwingL = 0.7f;
                    c.KneeBendL = 0.8f;
                    c.LegSwingR = -0.1f;
                    c.KneeBendR = 0.1f;
                    break;
                case AnimPose.Dunk:
                    SetArms(ref c, raise: 1f, elbow: Lerp(0.6f, 0.1f, t), swing: 0.3f);
                    c.KneeBendL = c.KneeBendR = 0.5f;
                    c.LegSwingL = c.LegSwingR = 0.2f;
                    break;
                case AnimPose.Pass:
                    // Chest pass: elbows push out.
                    SetArms(ref c, raise: 0.35f, elbow: Lerp(0.75f, 0f, t), swing: 0.9f);
                    c.SpineLean = Max(c.SpineLean, 0.2f);
                    break;
                case AnimPose.Block:
                    SetArms(ref c, raise: 1f, elbow: 0f, swing: 0.2f);
                    c.KneeBendL = c.KneeBendR = 0.3f;
                    break;
                case AnimPose.Airborne:
                    SetArms(ref c, raise: 0.55f, elbow: 0.3f, swing: 0.1f);
                    c.KneeBendL = c.KneeBendR = 0.4f;
                    c.LegSwingL = c.LegSwingR = 0.15f;
                    break;
                case AnimPose.Celebrate:
                    c.ArmRaiseR = 1f;
                    c.ElbowBendR = 0.4f + 0.2f * Mathf.Abs(Mathf.Sin(p.StridePhase * 2f));
                    c.ArmRaiseL = 0.3f;
                    break;
            }
            return c;
        }

        // Walk/run cycle: legs swing opposite each other, arms opposite the legs; the
        // amplitude and knee lift grow with speed.
        private static PoseChannels Locomotion(in PoseParams p)
        {
            var c = new PoseChannels();
            float amp = 0.55f * Clamp01(p.SpeedRatio);
            float s = Mathf.Sin(p.StridePhase);
            c.LegSwingL = amp * s;
            c.LegSwingR = -amp * s;
            // The swinging (forward-moving) leg bends; a standing player keeps a soft knee.
            c.KneeBendL = 0.08f + amp * Max(0f, Mathf.Cos(p.StridePhase)) * 1.2f;
            c.KneeBendR = 0.08f + amp * Max(0f, -Mathf.Cos(p.StridePhase)) * 1.2f;
            c.ArmSwingL = -amp * 0.8f * s;
            c.ArmSwingR = amp * 0.8f * s;
            c.ElbowBendL = c.ElbowBendR = 0.15f + 0.45f * Clamp01(p.SpeedRatio);
            c.SpineLean = 0.2f * Clamp01(p.SpeedRatio);
            c.Crouch = 0.05f;
            return c;
        }

        private static void SetArms(ref PoseChannels c, float raise, float elbow, float swing)
        {
            c.ArmRaiseL = c.ArmRaiseR = raise;
            c.ElbowBendL = c.ElbowBendR = elbow;
            c.ArmSwingL = c.ArmSwingR = swing;
        }

        // Feet under the body (no stride) during a set shot.
        private static void Stand(ref PoseChannels c)
        {
            c.LegSwingL = c.LegSwingR = 0f;
            c.KneeBendL = c.KneeBendR = c.Crouch;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
        private static float Max(float a, float b) => a > b ? a : b;
        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
