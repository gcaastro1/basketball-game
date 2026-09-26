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
        // A loose ball caught at most this high above the feet is picked up off the floor.
        private const float PickUpMaxBallHeight = 0.7f;

        private PlayerEntity player;
        private MatchSimulation sim;
        private Animator animator;
        private ProceduralHumanoidAnimator procedural;
        private ClipAnimationBackend clipBackend;
        private HandIK rightHand, leftHand;
        private Transform leftSole, rightSole;
        private float soleHeight;
        private float stridePhase, dribblePhase;
        private float sincePass = 99f, sinceScored = 99f, sincePickUp = 99f;
        private bool wasHolding;
        private float looseBallHeight = 99f;
        private float overlayWeight;
        private float lastYaw;
        private bool hasYaw;

        public AnimPose CurrentPose { get; private set; }
        public bool UsesClips => clipBackend != null;
        // Procedural only (no clips); with clips it still covers the poses they lack.
        public bool UsesProcedural => procedural != null && clipBackend == null;
        // Practice court panel: pose, body speed/turn and what the animation backend plays.
        public string DebugDescription()
        {
            if (player == null) return "";
            Vector3 v = player.Motor != null ? player.Motor.HorizontalVelocity : Vector3.zero;
            string head = $"pose {CurrentPose}  speed {new Vector2(v.x, v.z).magnitude:0.00} m/s  overlay {overlayWeight:0.00}\n";
            if (clipBackend != null) return head + clipBackend.Describe();
            return head + (procedural != null ? "procedural animation (no clips)" : "no humanoid rig");
        }

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

        private bool motor0Grounded() => player.Motor == null || player.Motor.IsGrounded;

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
            sincePickUp += dt;
            // A loose ball caught low (off the floor, not in the air): the pick-up pose.
            bool holdingNow = sim.Ball.CurrentHolder == player.transform;
            if (holdingNow && !wasHolding && looseBallHeight - player.FeetPosition.y < PickUpMaxBallHeight && motor0Grounded())
                sincePickUp = 0f;
            wasHolding = holdingNow;
            looseBallHeight = sim.Ball.CurrentState == BallState.Free ? sim.Ball.Position.y : 99f;

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
                Dribbling = sim.Ball.CurrentHolder == player.transform && sim.BallDribbling,
                PickingUp = sincePickUp < AnimationStateMapper.PickUpSeconds,
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
                : output.Pose == AnimPose.PickUp ? sincePickUp / AnimationStateMapper.PickUpSeconds
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
                clipBackend.Update(output.Pose, move, dt, shooting ? shotProgress : -1f, input.Dribbling ? sim.DribbleBounces : -1f);
                // Procedural only where the clips have nothing: fade it in and out.
                bool covered = output.Pose == AnimPose.Locomotion || clipBackend.HasClipFor(output.Pose);
                overlayWeight = Mathf.MoveTowards(overlayWeight, covered ? 0f : 1f, dt / OverlayFadeSeconds);
                // The pick-up is short and starts bent down: no fade in, or it never bends.
                if (output.Pose == AnimPose.PickUp && sincePickUp <= dt) overlayWeight = 1f;
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
            if (!input.HasBall) return;
            // With a ball model (BallVisualFollower) the ball goes to the hands when it is held
            // or gathered for a shot: the clip's arms stay as recorded. Otherwise (and always
            // while dribbling) the hands reach for the gameplay ball.
            bool ballFollowsHands = output.Pose != AnimPose.Dribble && sim.Ball.transform.Find(ArenaDresser.BallModelName) != null
                                    && TryHoldPoint(output.Pose, out heldBallCenter);
            if (ballFollowsHands) heldBallFrame = Time.frameCount;
            else HandsOnBall(output.Pose);
        }

        private Vector3 heldBallCenter;
        private int heldBallFrame = -1;

        // Where the ball is in this character's hands this frame (for the ball model), when it
        // follows the hands rather than the other way round.
        public bool TryGetHeldBallCenter(out Vector3 center)
        {
            center = heldBallCenter;
            return heldBallFrame == Time.frameCount;
        }

        // Holding: between the palms. Shooting: on the shooting (right) palm, facing up and
        // toward the rim, so the ball sits in the palm and not on the fingertips.
        private bool TryHoldPoint(AnimPose pose, out Vector3 center)
        {
            center = default;
            if (animator == null || !animator.isHuman) return false;
            float r = sim.Ball.Radius;
            Vector3 right = Palm(true, out Vector3 rightNormal);
            if (right == Vector3.zero) return false;
            Transform body = player.transform;
            bool shot = pose == AnimPose.JumpShot || pose == AnimPose.FreeThrow || pose == AnimPose.Layup || pose == AnimPose.Dunk;
            if (shot)
            {
                Vector3 up = (Vector3.up + body.forward * 0.5f).normalized;
                Vector3 n = rightNormal.sqrMagnitude > 0.5f ? rightNormal : up;
                if (Vector3.Dot(n, up) < 0f) n = -n;
                center = right + n * r;
                return true;
            }
            Vector3 left = Palm(false, out _);
            if (left == Vector3.zero) return false;
            center = (right + left) * 0.5f;
            // Hands closer than the ball: push it out in front of them.
            float gap = Vector3.Distance(right, left);
            if (gap < 2f * r) center += body.forward * Mathf.Sqrt(Mathf.Max(0f, r * r - gap * gap * 0.25f));
            return true;
        }

        // Middle of the palm (toward the middle finger's base) and its normal when the rig has
        // finger bones (sign unknown: the caller orients it).
        private Vector3 Palm(bool right, out Vector3 normal)
        {
            normal = Vector3.zero;
            Transform hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (hand == null) return Vector3.zero;
            Transform middle = animator.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            Transform index = animator.GetBoneTransform(right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
            Transform little = animator.GetBoneTransform(right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
            if (index != null && little != null)
                normal = Vector3.Cross(index.position - hand.position, little.position - hand.position).normalized;
            if (middle != null) return Vector3.Lerp(hand.position, middle.position, 0.6f);
            Transform lower = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            return lower != null ? hand.position + (hand.position - lower.position).normalized * 0.06f : hand.position;
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
