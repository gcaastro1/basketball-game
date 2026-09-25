using UnityEngine;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;

        private CharacterController controller;
        private Vector3 velocity;

        public Vector3 Velocity => velocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Tick(Vector2 moveInput, bool sprint, float dt)
        {
            Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = config.maxSpeed * (sprint ? config.sprintMultiplier : 1f);
            velocity = PlayerMotorMath.ComputeVelocity(velocity, desiredDir, speed, config.acceleration, config.deceleration, dt);

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, config.turnSpeedDegrees * dt);
            }

            // CharacterController.SimpleMove(Vector3) silently re-multiplies its argument
            // by Unity's real Time.deltaTime internally, discarding the dt this method was
            // given and making movement depend on real frame timing instead of the caller's
            // explicit dt. Use Move() with a pre-scaled motion vector instead, which moves by
            // exactly the vector given, so a test driving Tick() with a fixed synthetic dt
            // gets deterministic, real-timing-independent displacement. isGrounded needs a
            // continuous small downward push to read true on flat ground (no floor exists
            // in Task 5's unit test, so it free-falls slowly there instead — that's fine,
            // only horizontal displacement is asserted).
            // The stick push is dt-scaled so its cumulative effect doesn't depend on
            // frame rate. The airborne branch below is a deliberately simplified
            // displacement-only fall (not real velocity-integrated gravity) — this
            // slice has no jump/fall gameplay (see Global Constraints), so the
            // player is always grounded in practice and this branch is effectively
            // dead code; do not spend design effort on it here. If a future task
            // actually needs real airborne physics, replace this with a persisted
            // vertical-velocity field integrated each frame, not a bigger patch to
            // this line.
            Vector3 motion = velocity * dt;
            motion.y = controller.isGrounded ? -0.05f * dt : Physics.gravity.y * dt;
            controller.Move(motion);
        }

        internal void SetConfigForTest(PlayerMovementConfig testConfig)
        {
            config = testConfig;
        }
    }
}
