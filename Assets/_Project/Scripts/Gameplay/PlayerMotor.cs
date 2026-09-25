using UnityEngine;

namespace Basket.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        // Small constant downward speed while grounded so CharacterController keeps
        // reporting ground contact on flat floors.
        private const float GroundStickSpeed = 1f;

        [SerializeField] private PlayerMovementConfig config;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private bool grounded;

        public PlayerMovementConfig Config => config;
        public bool IsGrounded => grounded;
        public Vector3 HorizontalVelocity => horizontalVelocity;
        // Includes vertical velocity while airborne.
        public Vector3 Velocity => horizontalVelocity + Vector3.up * (grounded ? 0f : verticalVelocity);
        public float JumpSpeed => Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * config.jumpHeight);

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void Configure(PlayerMovementConfig movementConfig)
        {
            config = movementConfig;
        }

        public void Tick(Vector2 moveInput, bool sprint, float dt)
        {
            Vector3 desiredDir = new Vector3(moveInput.x, 0f, moveInput.y);
            float speed = config.maxSpeed * (sprint ? config.sprintMultiplier : 1f);

            if (grounded)
            {
                horizontalVelocity = PlayerMotorMath.ComputeVelocity(horizontalVelocity, desiredDir, speed, config.acceleration, config.deceleration, dt);
            }
            else if (desiredDir.sqrMagnitude > 0.0001f)
            {
                // Airborne: momentum is kept; input only steers a little.
                horizontalVelocity = PlayerMotorMath.ComputeVelocity(horizontalVelocity, desiredDir, speed, config.acceleration * config.airControl, 0f, dt);
            }

            if (horizontalVelocity.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalVelocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, config.turnSpeedDegrees * dt);
            }

            // Move() (not SimpleMove) so displacement uses exactly the dt given: tests that
            // drive Tick() with a synthetic dt get deterministic results.
            if (grounded && verticalVelocity <= 0f) verticalVelocity = -GroundStickSpeed;
            else verticalVelocity += Physics.gravity.y * dt;

            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            bool hitGround = (flags & CollisionFlags.Below) != 0;
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;

            grounded = hitGround && verticalVelocity <= 0f;
            if (grounded) verticalVelocity = 0f;
        }

        public bool Jump() => Jump(horizontalVelocity);

        // Jump with an explicit horizontal takeoff velocity (e.g. a dunk lunge).
        public bool Jump(Vector3 takeoffHorizontalVelocity)
        {
            if (!grounded) return false;
            takeoffHorizontalVelocity.y = 0f;
            horizontalVelocity = takeoffHorizontalVelocity;
            verticalVelocity = JumpSpeed;
            grounded = false;
            return true;
        }

        // Moving a CharacterController's transform directly is overwritten by its internal
        // state on the next Move(); it has to be disabled while repositioning.
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = wasEnabled;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            grounded = true;
        }

        internal void SetConfigForTest(PlayerMovementConfig testConfig) => Configure(testConfig);
    }
}
