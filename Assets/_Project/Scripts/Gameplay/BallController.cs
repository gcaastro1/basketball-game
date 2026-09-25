using System;
using System.Collections.Generic;
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

        // A pass in flight: who it is thrown to, and the players around the passer it is
        // thrown past (a real pass goes around or over the on-ball defender; without this,
        // the defender standing next to the passer "caught" nearly every pass at release --
        // found by the AI-vs-AI simulation).
        private Transform passReceiver;
        private readonly List<Collider> passIgnoredColliders = new List<Collider>();

        // The release that can still score: set by Release(), cleared when the ball touches
        // the floor, is caught, or is reset. A shot that hits the rim and then drops in is
        // still "live"; one that bounced off the floor first is not.
        private bool releaseLive;
        private PlayerEntity releaseEntity;
        private Vector3 releasePosition;
        private BallState releaseKind;
        private ShotType? releaseShotType;

        public BallState CurrentState => stateMachine.CurrentState;
        public Vector3 Position => transform.position;
        // Position as the physics engine sees it (use from FixedUpdate).
        public Vector3 PhysicsPosition => rb != null ? rb.position : transform.position;
        public Transform CurrentHolder { get; private set; }
        // Team of the last player to hold/release the ball; null if never touched by a PlayerEntity.
        public TeamId? LastTouchTeam { get; private set; }
        public bool HasLiveRelease => releaseLive;
        public Vector3 Velocity => rb != null && !rb.isKinematic ? rb.linearVelocity : Vector3.zero;
        // Time.time of the last release or knock-loose.
        public float LastReleaseTime => lastReleaseTime;
        public float Radius => ballCollider is SphereCollider sphere ? sphere.radius * MaxAbsScale() : 0.12f;

        public event Action<BallState, BallState> OnStateChanged
        {
            add => stateMachine.OnStateChanged += value;
            remove => stateMachine.OnStateChanged -= value;
        }
        public event Action<ScoreEvent> OnScored;
        public event Action OnRimTouched;

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
            EndPassProtection();
            passReceiver = null;

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

        public void Release(BallState releaseState, Vector3 velocity, ShotType? shotType = null)
        {
            if (stateMachine.CurrentState != BallState.Held) return;
            if (!stateMachine.TryTransition(releaseState)) return;

            lastReleasedBy = CurrentHolder;
            lastReleaseTime = Time.time;
            releaseLive = true;
            releaseEntity = holderEntity;
            releasePosition = holderEntity != null ? holderEntity.FeetPosition : CurrentHolder.position;
            releaseKind = releaseState;
            releaseShotType = shotType;

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

        // Release from an explicit position (e.g. a dunk puts the ball above the rim).
        public void ReleaseAt(BallState releaseState, Vector3 position, Vector3 velocity, ShotType? shotType = null)
        {
            if (stateMachine.CurrentState != BallState.Held) return;
            Release(releaseState, velocity, shotType);
            transform.position = position;
            rb.position = position;
        }

        // Steal / block: the ball is knocked away and becomes a loose ball. It cannot score
        // from this touch, and whoever last had it cannot re-grab it for the grace period.
        public void KnockLoose(Vector3 velocity, TeamId? touchedBy)
        {
            Transform previous = CurrentHolder != null ? CurrentHolder : lastReleasedBy;
            PlayerEntity previousEntity = holderEntity;

            stateMachine.ForceState(BallState.Free);
            CurrentHolder = null;
            holderEntity = null;
            releaseLive = false;
            heldLocalOffset = Vector3.zero;
            lastReleasedBy = previous;
            lastReleaseTime = Time.time;
            if (touchedBy.HasValue) LastTouchTeam = touchedBy;

            RestoreReleaserCollision();
            EndPassProtection();
            passReceiver = null;
            if (ballCollider != null)
            {
                ballCollider.enabled = true;
                if (previousEntity != null && previousEntity.BodyCollider != null)
                {
                    ignoredReleaserCollider = previousEntity.BodyCollider;
                    Physics.IgnoreCollision(ballCollider, ignoredReleaserCollider, true);
                }
            }
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
        public void NotifyScored() => NotifyScored(null, null);

        public void NotifyScored(Vector3? hoopCenter, TeamId? hoopTeam)
        {
            if (!releaseLive) return;
            releaseLive = false;
            TeamId? team = releaseEntity != null ? releaseEntity.Team : (TeamId?)null;
            OnScored?.Invoke(new ScoreEvent(releaseEntity, team, releasePosition, releaseKind, releaseShotType, hoopCenter, hoopTeam));
        }

        // Jump ball: the ball goes up from `position`, loose, owned by nobody.
        public void Toss(Vector3 position, Vector3 velocity)
        {
            stateMachine.ForceState(BallState.Free);
            CurrentHolder = null;
            holderEntity = null;
            releaseLive = false;
            heldLocalOffset = Vector3.zero;
            lastReleasedBy = null;
            LastTouchTeam = null;
            RestoreReleaserCollision();
            EndPassProtection();
            passReceiver = null;
            if (ballCollider != null) ballCollider.enabled = true;
            rb.isKinematic = false;
            SetFlightPhysics();
            transform.position = position;
            rb.position = position;
            rb.linearVelocity = velocity;
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
            // The protection lasts the whole pass: it ends when the pass does (caught, or
            // loose after touching anything).
            if (passIgnoredColliders.Count > 0 && stateMachine.CurrentState != BallState.Passing)
            {
                EndPassProtection();
            }
        }

        // Called right after a pass release. Players (other than the receiver) within
        // BallConfig.passProtectRadius of the ball -- the passer's own defender -- cannot
        // touch this pass: it goes over/around them. Anyone else in the lane (e.g. the
        // receiver's defender) can still intercept it. A 0.2 s window was not enough: the
        // AI-vs-AI log showed 8 of 10 lost passes caught by a defender 0.7-1.1 m from the
        // passer, standing on the passing line.
        public void BeginPass(Transform receiver, IReadOnlyList<Collider> nearbyPlayers)
        {
            if (stateMachine.CurrentState != BallState.Passing) return;
            passReceiver = receiver;
            EndPassProtection();
            if (ballCollider == null || nearbyPlayers == null) return;
            for (int i = 0; i < nearbyPlayers.Count; i++)
            {
                Collider c = nearbyPlayers[i];
                if (c == null || c == ignoredReleaserCollider || c.transform == receiver) continue;
                Physics.IgnoreCollision(ballCollider, c, true);
                passIgnoredColliders.Add(c);
            }
        }

        private void EndPassProtection()
        {
            if (ballCollider != null)
            {
                foreach (Collider c in passIgnoredColliders)
                {
                    if (c != null) Physics.IgnoreCollision(ballCollider, c, false);
                }
            }
            passIgnoredColliders.Clear();
        }

        private bool InPassProtection(Transform player)
        {
            if (stateMachine.CurrentState != BallState.Passing || player == passReceiver) return false;
            foreach (Collider c in passIgnoredColliders)
            {
                if (c != null && c.transform == player) return true;
            }
            return false;
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
            if (IsCatchable && collision.transform.TryGetComponent<PlayerEntity>(out _) && !InSelfCatchGrace(collision.transform)
                && !InPassProtection(collision.transform))
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
            else if (collision.transform.TryGetComponent<RimSurface>(out _))
            {
                OnRimTouched?.Invoke();
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
            if (InPassProtection(player)) return false;
            if (!player.TryGetComponent<PlayerEntity>(out var entity))
            {
                return Vector3.Distance(transform.position, player.position) <= config.catchRadius;
            }
            // A PlayerEntity must be able to reach the ball: horizontally close, and not
            // above their fingertips (jumping raises them -- that is how rebounds are won).
            float heightAboveFeet = transform.position.y - entity.FeetPosition.y;
            if (heightAboveFeet > entity.StandingReach + config.catchReachMargin || heightAboveFeet < -0.2f) return false;
            // Good rebounders get to more loose balls.
            float radius = config.catchRadius;
            // A pass is caught by its receiver's full reach; anyone else has to be in the
            // lane to pick it off.
            if (stateMachine.CurrentState == BallState.Passing && passReceiver != null && player != passReceiver)
                radius = config.interceptRadius;
            if (stateMachine.CurrentState == BallState.Free && entity.Tuning != null)
                radius *= entity.AttributeMult(AttributeId.DefensiveRebound, entity.Tuning.reboundRadius);
            return DistanceTo(player) <= radius;
        }

        // Horizontal distance for players (height is checked separately by reach).
        public float DistanceTo(Transform player)
        {
            if (player.TryGetComponent<PlayerEntity>(out var entity))
            {
                Vector3 d = transform.position - entity.FeetPosition;
                d.y = 0f;
                return d.magnitude;
            }
            return Vector3.Distance(transform.position, player.position);
        }

        private float MaxAbsScale()
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        }

        internal void SetConfigForTest(BallConfig testConfig) => Configure(testConfig);
    }
}
