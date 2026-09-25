using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour, IBallStateReadOnly
    {
        [SerializeField] private BallConfig config;

        // Right after a release the ball is still next to the releaser. For this long it
        // ignores collisions with them and cannot be re-caught by them, so a pass or shot
        // is never silently cancelled by the releaser's own body.
        private const float SelfCatchGraceSeconds = 0.25f;

        private Rigidbody rb;
        private Collider ballCollider;
        private readonly BallPossessionStateMachine stateMachine = new();
        private Vector3 heldLocalOffset;
        private PlayerEntity holderEntity;

        private Transform lastReleasedBy;
        private float lastReleaseTime = float.NegativeInfinity;
        private Collider ignoredReleaserCollider;

        // The release that can still score: set by Release(), cleared when the ball touches
        // the floor, is caught, or is reset. A shot that hits the rim and then drops in is
        // still "live"; one that bounced off the floor first is not.
        private bool releaseLive;
        private PlayerEntity releaseEntity;
        private Vector3 releasePosition;
        private BallState releaseKind;

        public BallState CurrentState => stateMachine.CurrentState;
        public Vector3 Position => transform.position;
        // Position as the physics engine sees it (use from FixedUpdate).
        public Vector3 PhysicsPosition => rb != null ? rb.position : transform.position;
        public Transform CurrentHolder { get; private set; }
        // Team of the last player to hold/release the ball; null if never touched by a PlayerEntity.
        public TeamId? LastTouchTeam { get; private set; }
        public bool HasLiveRelease => releaseLive;
        public float Radius => ballCollider is SphereCollider sphere ? sphere.radius * MaxAbsScale() : 0.12f;

        public event Action<BallState, BallState> OnStateChanged
        {
            add => stateMachine.OnStateChanged += value;
            remove => stateMachine.OnStateChanged -= value;
        }
        public event Action<ScoreEvent> OnScored;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();
            // config may not be assigned yet when Awake fires synchronously from
            // AddComponent (e.g. in tests that call SetConfigForTest afterwards);
            // ApplyConfigToRigidbody re-applies once a config is actually set.
            if (config != null) ApplyConfigToRigidbody();
        }

        public void Configure(BallConfig ballConfig)
        {
            config = ballConfig;
            if (rb != null) ApplyConfigToRigidbody();
        }

        private void ApplyConfigToRigidbody()
        {
            rb.mass = config.mass;
            rb.linearDamping = config.drag;
            rb.angularDamping = config.angularDrag;
            if (!rb.isKinematic) SetFlightPhysics();
        }

        // Continuous detection: the rim is a ring of 1 cm tubes and the ball moves ~0.2 m
        // per physics step on a shot, so discrete detection would tunnel through it.
        private void SetFlightPhysics()
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        public float LinearDamping => rb != null ? rb.linearDamping : 0f;

        public void Catch(Transform holder)
        {
            if (!stateMachine.TryTransition(BallState.Held)) return;
            TakeHold(holder);
        }

        // Rule-driven possession (check ball / inbound): gives the ball to holder from any state.
        public void ResetToHolder(Transform holder)
        {
            stateMachine.ForceState(BallState.Held);
            TakeHold(holder);
        }

        private void TakeHold(Transform holder)
        {
            CurrentHolder = holder;
            holder.TryGetComponent(out holderEntity);
            if (holderEntity != null) LastTouchTeam = holderEntity.Team;
            heldLocalOffset = Vector3.zero;
            releaseLive = false;
            RestoreReleaserCollision();

            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // Kinematic bodies only support discrete/speculative detection; switch
                // before making it kinematic to avoid a runtime warning.
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                // Interpolation would fight the per-frame transform writes in LateUpdate.
                rb.interpolation = RigidbodyInterpolation.None;
                rb.isKinematic = true;
            }
            // While held the ball is part of the holder: no collisions with anyone
            // (including the holder's own CharacterController, which it overlaps).
            if (ballCollider != null) ballCollider.enabled = false;
            transform.position = HeldPosition();
        }

        public void Release(BallState releaseState, Vector3 velocity)
        {
            if (stateMachine.CurrentState != BallState.Held) return;
            if (!stateMachine.TryTransition(releaseState)) return;

            lastReleasedBy = CurrentHolder;
            lastReleaseTime = Time.time;
            releaseLive = true;
            releaseEntity = holderEntity;
            releasePosition = holderEntity != null ? holderEntity.FeetPosition : CurrentHolder.position;
            releaseKind = releaseState;

            if (ballCollider != null)
            {
                ballCollider.enabled = true;
                if (holderEntity != null && holderEntity.BodyCollider != null)
                {
                    ignoredReleaserCollider = holderEntity.BodyCollider;
                    Physics.IgnoreCollision(ballCollider, ignoredReleaserCollider, true);
                }
            }

            CurrentHolder = null;
            holderEntity = null;
            rb.isKinematic = false;
            SetFlightPhysics();
            rb.linearVelocity = velocity;
        }

        public void SetHeldLocalOffset(Vector3 offset)
        {
            heldLocalOffset = offset;
        }

        // Called by the hoop when the ball passes down through the rim. Only a live
        // release can score.
        public void NotifyScored()
        {
            if (!releaseLive) return;
            releaseLive = false;
            TeamId? team = releaseEntity != null ? releaseEntity.Team : (TeamId?)null;
            OnScored?.Invoke(new ScoreEvent(releaseEntity, team, releasePosition, releaseKind));
        }

        private Vector3 HeldPosition()
        {
            if (holderEntity != null)
            {
                Transform t = holderEntity.transform;
                return holderEntity.FeetPosition
                       + Vector3.up * config.holdHeightAboveFeet
                       + t.forward * config.holdForwardOffset
                       + t.right * config.holdSideOffset
                       + heldLocalOffset;
            }
            return CurrentHolder.position + Vector3.up * config.handHeightOffset + heldLocalOffset;
        }

        private void LateUpdate()
        {
            if (stateMachine.CurrentState == BallState.Held && CurrentHolder != null)
            {
                transform.position = HeldPosition();
            }
        }

        private void FixedUpdate()
        {
            if (ignoredReleaserCollider != null && Time.time - lastReleaseTime >= SelfCatchGraceSeconds)
            {
                RestoreReleaserCollision();
            }
        }

        private void RestoreReleaserCollision()
        {
            if (ignoredReleaserCollider != null && ballCollider != null)
            {
                Physics.IgnoreCollision(ballCollider, ignoredReleaserCollider, false);
            }
            ignoredReleaserCollider = null;
        }

        private bool IsCatchable => stateMachine.CurrentState == BallState.Free || stateMachine.CurrentState == BallState.Passing;

        private bool InSelfCatchGrace(Transform player) =>
            player == lastReleasedBy && Time.time - lastReleaseTime < SelfCatchGraceSeconds;

        private void OnCollisionEnter(Collision collision)
        {
            BallState state = stateMachine.CurrentState;
            if (state == BallState.Held) return;

            // A pass (or loose ball) that lands on a player is caught -- that is also how
            // interceptions happen. A shot is never caught out of the air.
            if (IsCatchable && collision.transform.TryGetComponent<PlayerEntity>(out _) && !InSelfCatchGrace(collision.transform))
            {
                Catch(collision.transform);
                return;
            }

            if (state == BallState.Shooting || state == BallState.Passing)
            {
                stateMachine.TryTransition(BallState.Free);
            }
            if (collision.transform.TryGetComponent<CourtSurface>(out _))
            {
                releaseLive = false;
            }
        }

        // CharacterController does not reliably fire OnCollisionEnter on the other party's
        // Rigidbody when it walks into it -- PhysX's character-controller sweep runs through
        // a different collision path than normal Rigidbody-vs-Rigidbody/Collider contacts, and
        // in practice a player can stand directly on a loose ball without OnCollisionEnter ever
        // firing (confirmed during manual playtesting: the AI walked onto a free ball and it
        // was never picked up). This proximity check is the reliable pickup path for any
        // CharacterController-driven player; OnCollisionEnter above is left in place as a
        // secondary path for a fast-moving pass that happens to land on someone.
        public bool TryCatchNearby(Transform player)
        {
            if (!CanBeCaughtBy(player)) return false;
            Catch(player);
            return true;
        }

        public bool CanBeCaughtBy(Transform player)
        {
            if (!IsCatchable) return false;
            if (InSelfCatchGrace(player)) return false;
            return DistanceTo(player) <= config.catchRadius;
        }

        public float DistanceTo(Transform player)
        {
            Vector3 anchor = player.TryGetComponent<PlayerEntity>(out var entity)
                ? entity.FeetPosition + Vector3.up * config.holdHeightAboveFeet
                : player.position;
            return Vector3.Distance(transform.position, anchor);
        }

        private float MaxAbsScale()
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        }

        internal void SetConfigForTest(BallConfig testConfig) => Configure(testConfig);
    }
}
