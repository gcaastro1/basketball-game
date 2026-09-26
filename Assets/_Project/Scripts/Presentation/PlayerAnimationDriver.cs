using UnityEngine;
using Basket.Characters;
using Basket.Core;
using Basket.Gameplay;

namespace Basket.Presentation
{
    // Reads one player's gameplay state every frame and animates their model: real clips
    // when the character has them (locomotion, dribble, ...), procedural muscles for every
    // pose that still has no clip (blended over the clips), then hand IK onto the ball.
    // Runs after BallController.LateUpdate so the ball is already where gameplay put it.
    [DefaultExecutionOrder(100)]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        private const float StrideLengthMeters = 2.2f;
        // Cycles of the procedural pump (|sin|: two pumps per cycle) = 1.2 bounces/s, the ball's.
        private const float DribbleHz = 0.6f;
        private const float TwoPi = Mathf.PI * 2f;
        private const float ToeHeight = 0.03f;
        private const float OverlayFadeSeconds = 0.15f;

        private PlayerEntity player;
        private MatchSimulation sim;
        private Animator animator;
        private ProceduralHumanoidAnimator procedural;
        private ClipAnimationBackend clipBackend;
        private HandIK rightHand, leftHand;
        private Transform leftSole, rightSole;
        private float soleHeight;
        private float stridePhase, dribblePhase;
        private float sincePass = 99f, sinceScored = 99f;
        private float overlayWeight;
        private float lastYaw;
        private bool hasYaw;

        public AnimPose CurrentPose { get; private set; }
        public bool UsesClips => clipBackend != null;
        // Procedural only (no clips); with clips it still covers the poses they lack.
        public bool UsesProcedural => procedural != null && clipBackend == null;
        public Vector3 RightHandPosition => rightHand != null ? rightHand.HandPosition : transform.position;
        public Vector3 LeftHandPosition => leftHand != null ? leftHand.HandPosition : transform.position;

        public void Configure(PlayerEntity entity, MatchSimulation simulation, Animator bodyAnimator, CharacterAnimationClips clips,
            float soleBelowToes = ToeHeight)
        {
            player = entity;
            sim = simulation;
            animator = bodyAnimator;
            bool humanoid = animator != null && animator.avatar != null && animator.avatar.isHuman;
            if (humanoid && clips != null && clips.HasLocomotion) clipBackend = new ClipAnimationBackend(animator, clips);
            if (humanoid) procedural = new ProceduralHumanoidAnimator(animator);
            if (humanoid)
            {
                rightHand = new HandIK(animator, right: true);
                leftHand = new HandIK(animator, right: false);
                        leftSole = animator.GetBoneTransform(HumanBodyBones.LeftToes);
                rightSole = animator.GetBoneTransform(HumanBodyBones.RightToes);
                if (leftSole == null || rightSole == null)
                {
                    leftSole = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    rightSole = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                }
                // Measured on the mesh when available (Tripo's soles sit well below the toe joint).
                soleHeight = soleBelowToes > 0f ? soleBelowToes : ToeHeight;
            }
            if (sim != null)
            {
                sim.OnPassThrown += OnPassThrown;
                sim.Match.OnBasketCounted += OnBasketCounted;
            }
        }

        private void OnPassThrown(int passer)
        {
            if (player != null && passer == player.Index) sincePass = 0f;
        }

        private void OnBasketCounted(TeamId team, int points, ShotType? type)
        {
            if (player != null && team == player.Team) sinceScored = 0f;
        }

        private void LateUpdate()
        {
            if (player == null || sim == null) return;
            float dt = Time.deltaTime;
            sincePass += dt;
            sinceScored += dt;

            bool shooting = sim.TryGetShot(player.Index, out ShotType shotType, out float shotProgress);
            PlayerMotor motor = player.Motor;
            TeamId? possession = sim.Match.State.PossessionTeam;
            var input = new PlayerAnimInput
            {
                Speed = motor.HorizontalVelocity.magnitude,
                MaxSpeed = motor.MaxSpeed,
                Grounded = motor.IsGrounded,
                VerticalVelocity = motor.Velocity.y,
                HasBall = sim.Ball.CurrentHolder == player.transform,
                Shooting = shooting,
                ShotType = shotType,
                SincePass = sincePass,
                SinceTeamScored = sinceScored,
                Defending = possession.HasValue && possession.Value != player.Team,
                Guarding = player.IsGuarding,
            };
            PlayerAnimOutput output = AnimationStateMapper.Map(input);
            CurrentPose = output.Pose;

            stridePhase = (stridePhase + input.Speed * dt * TwoPi / StrideLengthMeters) % TwoPi;
            dribblePhase = (dribblePhase + dt * DribbleHz * TwoPi) % TwoPi;
            float actionT = shooting ? shotProgress
                : output.Pose == AnimPose.Pass ? sincePass / AnimationStateMapper.PassPoseSeconds
                : 0f;

            float overlay = 1f;
            if (clipBackend != null)
            {
                Vector3 velocity = motor.HorizontalVelocity;
                Transform body = player.transform;
                float yaw = body.eulerAngles.y;
                float yawRate = hasYaw && dt > 0f ? Mathf.DeltaAngle(lastYaw, yaw) / dt : 0f;
                lastYaw = yaw;
                hasYaw = true;
                var move = new LocomotionInput
                {
                    Speed = input.Speed,
                    Forward = Vector3.Dot(velocity, body.forward),
                    Right = Vector3.Dot(velocity, body.right),
                    YawRate = yawRate,
                };
                clipBackend.Update(output.Pose, move, dt, shooting ? shotProgress : -1f);
                // Procedural only where the clips have nothing: fade it in and out.
                bool covered = output.Pose == AnimPose.Locomotion || clipBackend.HasClipFor(output.Pose);
                overlayWeight = Mathf.MoveTowards(overlayWeight, covered ? 0f : 1f, dt / OverlayFadeSeconds);
                overlay = overlayWeight;
            }
            if (procedural != null && overlay > 0f)
            {
                procedural.Apply(ProceduralPoseMath.Compute(new PoseParams
                {
                    Pose = output.Pose,
                    SpeedRatio = output.SpeedRatio,
                    Grounded = input.Grounded,
                    StridePhase = stridePhase,
                    DribblePhase = dribblePhase,
                    ActionT = actionT,
                }), overlay);
            }

            GroundFeet();
            if (input.HasBall) HandsOnBall(output.Pose);
        }

        // Puts the lowest foot of the posed body on the gameplay body's feet (which rise in
        // a jump too). Mesh bounds alone left the Tripo model 1.3 m under the floor in CI:
        // the posed skeleton, not the bind-pose mesh, decides where the feet are.
        private void GroundFeet()
        {
            if (leftSole == null || rightSole == null) return;
            float lowest = Mathf.Min(leftSole.position.y, rightSole.position.y);
            float target = player.FeetPosition.y + soleHeight;
            transform.position += Vector3.up * (target - lowest);
        }

        private void HandsOnBall(AnimPose pose)
        {
            if (rightHand == null) return;
            BallController ball = sim.Ball;
            Vector3 b = ball.Position;
            float r = ball.Radius;
            Transform body = player.transform;
            if (pose == AnimPose.Dribble)
            {
                // Hand on top of the ball, following it down to about the knee.
                float height = b.y - player.FeetPosition.y;
                rightHand.Reach(b + Vector3.up * r, body, Mathf.Clamp01((height - 0.45f) / 0.35f));
                return;
            }
            // Each hand on the side of the ball facing its shoulder (layup: the shooting hand
            // leads). The ball is carried on the right, so the left hand reaches across.
            rightHand.Reach(b + (rightHand.ShoulderPosition - b).normalized * r, body, 1f);
            leftHand.Reach(b + (leftHand.ShoulderPosition - b).normalized * r, body, pose == AnimPose.Layup ? 0.5f : 1f);
        }

        private void OnDestroy()
        {
            if (sim != null)
            {
                sim.OnPassThrown -= OnPassThrown;
                sim.Match.OnBasketCounted -= OnBasketCounted;
            }
            procedural?.Dispose();
            clipBackend?.Dispose();
        }
    }
}
