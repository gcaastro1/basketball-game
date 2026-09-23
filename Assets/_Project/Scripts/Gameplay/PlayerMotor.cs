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

            controller.SimpleMove(velocity);
        }

        internal void SetConfigForTest(PlayerMovementConfig testConfig)
        {
            config = testConfig;
        }
    }
}
