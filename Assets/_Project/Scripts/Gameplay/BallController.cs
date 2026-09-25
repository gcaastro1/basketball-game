using System;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour, IBallStateReadOnly
    {
        [SerializeField] private BallConfig config;

        // A freshly-released ball starts touching the releasing player's own collider
        // (the hand socket sits inside their capsule). Without this grace window,
        // OnCollisionEnter would immediately re-catch the ball to the same player,
        // silently nullifying every pass and shot.
        private const float SelfCatchGraceSeconds = 0.25f;

        private Rigidbody rb;
        private readonly BallPossessionStateMachine stateMachine = new();
        private Vector3 heldLocalOffset;
        private Transform lastReleasedBy;
        private float lastReleaseTime = float.NegativeInfinity;

        public BallState CurrentState => stateMachine.CurrentState;
        public Vector3 Position => transform.position;
        public Transform CurrentHolder { get; private set; }

        public event Action<BallState, BallState> OnStateChanged
        {
            add => stateMachine.OnStateChanged += value;
            remove => stateMachine.OnStateChanged -= value;
        }
        public event Action<Transform> OnScored;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            // config may not be assigned yet when Awake fires synchronously from
            // AddComponent (e.g. in tests that call SetConfigForTest afterwards);
            // ApplyConfigToRigidbody re-applies once a config is actually set.
            if (config != null) ApplyConfigToRigidbody();
        }

        private void ApplyConfigToRigidbody()
        {
            rb.mass = config.mass;
            rb.linearDamping = config.drag;
            rb.angularDamping = config.angularDrag;
        }

        public void Catch(Transform holder)
        {
            if (!stateMachine.TryTransition(BallState.Held)) return;
            CurrentHolder = holder;
            rb.isKinematic = true;
            heldLocalOffset = Vector3.zero;
        }

        public void Release(BallState releaseState, Vector3 velocity)
        {
            if (stateMachine.CurrentState != BallState.Held) return;
            stateMachine.TryTransition(releaseState);
            lastReleasedBy = CurrentHolder;
            lastReleaseTime = Time.time;
            CurrentHolder = null;
            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            stateMachine.TryTransition(BallState.Free);
        }

        public void SetHeldLocalOffset(Vector3 offset)
        {
            heldLocalOffset = offset;
        }

        public void NotifyScored()
        {
            OnScored?.Invoke(lastReleasedBy);
        }

        private void LateUpdate()
        {
            if (stateMachine.CurrentState == BallState.Held && CurrentHolder != null)
            {
                transform.position = CurrentHolder.position + Vector3.up * config.handHeightOffset + heldLocalOffset;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (stateMachine.CurrentState != BallState.Free) return;
            if (collision.transform == lastReleasedBy && Time.time - lastReleaseTime < SelfCatchGraceSeconds) return;
            if (collision.transform.TryGetComponent<PlayerMarker>(out _))
            {
                Catch(collision.transform);
            }
        }

        // CharacterController does not reliably fire OnCollisionEnter on the other party's
        // Rigidbody when it walks into it -- PhysX's character-controller sweep runs through
        // a different collision path than normal Rigidbody-vs-Rigidbody/Collider contacts, and
        // in practice a player can stand directly on a loose ball without OnCollisionEnter ever
        // firing (confirmed during manual playtesting: the AI walked onto a free ball and it
        // was never picked up). This proximity check is the reliable pickup path for any
        // CharacterController-driven player; OnCollisionEnter above is left in place as a
        // harmless secondary path for a fast-moving pass/shot that happens to land on someone.
        public bool TryCatchNearby(Transform player)
        {
            if (stateMachine.CurrentState != BallState.Free) return false;
            if (player == lastReleasedBy && Time.time - lastReleaseTime < SelfCatchGraceSeconds) return false;
            if (Vector3.Distance(transform.position, player.position) > config.catchRadius) return false;

            Catch(player);
            return true;
        }

        internal void SetConfigForTest(BallConfig testConfig)
        {
            config = testConfig;
            if (rb != null) ApplyConfigToRigidbody();
        }
    }
}
