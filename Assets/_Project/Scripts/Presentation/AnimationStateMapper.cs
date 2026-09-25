using Basket.Core;

namespace Basket.Presentation
{
    // What a player's body is doing, as far as animation cares. Presentation only reads
    // gameplay; it never feeds back into it (Etapa 6: swapping visuals changes no rules).
    public enum AnimPose
    {
        Locomotion,
        Dribble,
        HoldBall,
        Defense,
        JumpShot,
        Layup,
        Dunk,
        Pass,
        Block,
        Airborne,
        Celebrate,
    }

    public struct PlayerAnimInput
    {
        public float Speed;
        public float MaxSpeed;
        public bool Grounded;
        public float VerticalVelocity;
        public bool HasBall;
        public bool Shooting;
        public ShotType ShotType;
        // Seconds since this player threw a pass / their team scored (large = never).
        public float SincePass;
        public float SinceTeamScored;
        // The other team has the ball.
        public bool Defending;
    }

    public readonly struct PlayerAnimOutput
    {
        public readonly AnimPose Pose;
        // 0 standing .. 1 at full speed.
        public readonly float SpeedRatio;

        public PlayerAnimOutput(AnimPose pose, float speedRatio)
        {
            Pose = pose;
            SpeedRatio = speedRatio;
        }
    }

    public static class AnimationStateMapper
    {
        public const float PassPoseSeconds = 0.35f;
        public const float CelebrateSeconds = 1.2f;
        // Below this speed ratio a ball handler holds the ball instead of dribbling.
        public const float DribbleSpeedRatio = 0.08f;

        public static PlayerAnimOutput Map(in PlayerAnimInput i)
        {
            float speedRatio = i.MaxSpeed > 0.01f ? Clamp01(i.Speed / i.MaxSpeed) : 0f;
            return new PlayerAnimOutput(PoseFor(i, speedRatio), speedRatio);
        }

        private static AnimPose PoseFor(in PlayerAnimInput i, float speedRatio)
        {
            if (i.Shooting)
            {
                switch (i.ShotType)
                {
                    case ShotType.Layup: return AnimPose.Layup;
                    case ShotType.Dunk: return AnimPose.Dunk;
                    default: return AnimPose.JumpShot;
                }
            }
            if (i.SincePass < PassPoseSeconds) return AnimPose.Pass;
            if (!i.Grounded) return i.Defending ? AnimPose.Block : AnimPose.Airborne;
            if (i.HasBall) return speedRatio > DribbleSpeedRatio ? AnimPose.Dribble : AnimPose.HoldBall;
            if (i.Defending) return AnimPose.Defense;
            if (i.SinceTeamScored < CelebrateSeconds && speedRatio < 0.5f) return AnimPose.Celebrate;
            return AnimPose.Locomotion;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
