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
        FreeThrow,
        Layup,
        Dunk,
        Pass,
        Block,
        Airborne,
        Celebrate,
        // Scooping a loose ball off the floor: crouched with the hands down, rising with it.
        PickUp,
    }

    public struct PlayerAnimInput
    {
        public float Speed;
        public float MaxSpeed;
        public bool Grounded;
        public float VerticalVelocity;
        public bool HasBall;
        // Holding the ball after starting a dribble (it keeps bouncing standing still).
        public bool Dribbling;
        // Just picked a loose ball up off the floor (for PickUpSeconds).
        public bool PickingUp;
        public bool Shooting;
        public ShotType ShotType;
        // Seconds since this player threw a pass / their team scored (large = never).
        public float SincePass;
        public float SinceTeamScored;
        // The other team has the ball.
        public bool Defending;
        // Holding the defensive guard stance (Guard button / AI on the ball).
        public bool Guarding;
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
        public const float PickUpSeconds = 0.4f;

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
                    case ShotType.FreeThrow: return AnimPose.FreeThrow;
                    default: return AnimPose.JumpShot;
                }
            }
            if (i.SincePass < PassPoseSeconds) return AnimPose.Pass;
            if (!i.Grounded) return i.Defending ? AnimPose.Block : AnimPose.Airborne;
            if (i.HasBall && i.PickingUp) return AnimPose.PickUp;
            // A dribbler keeps dribbling standing still; the ball is only held before the dribble.
            if (i.HasBall) return i.Dribbling || speedRatio > DribbleSpeedRatio ? AnimPose.Dribble : AnimPose.HoldBall;
            if (i.Guarding) return AnimPose.Defense;
            if (i.SinceTeamScored < CelebrateSeconds && speedRatio < 0.5f) return AnimPose.Celebrate;
            return AnimPose.Locomotion;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
