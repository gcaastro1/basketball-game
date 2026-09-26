using System;
using UnityEngine;

namespace Basket.Gameplay
{
    // Follows a player like a basketball broadcast camera: behind and above them, always
    // facing the basket being attacked (CameraRigMath). When the attacked basket changes
    // (full court), the camera swings around smoothly. Without a basket to face it falls
    // back to a plain offset follow.
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        private Transform target;
        private Func<Vector3> attackedHoop;
        private Vector3 courtCenter;
        private Vector3 direction;
        private float yaw;
        private bool placed;

        public Vector3 Direction => direction;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        public void Configure(Transform followTarget, CameraConfig cameraConfig)
        {
            config = cameraConfig;
            target = followTarget;
        }

        // attackedHoop: the rim the team with the ball is attacking (read every frame).
        public void Configure(Transform followTarget, CameraConfig cameraConfig, Func<Vector3> attackedHoop, Vector3 courtCenter)
        {
            Configure(followTarget, cameraConfig);
            this.attackedHoop = attackedHoop;
            this.courtCenter = courtCenter;
            placed = false;
            if (config != null && config.fieldOfView > 1f && TryGetComponent<Camera>(out var cam)) cam.fieldOfView = config.fieldOfView;
        }

        private void LateUpdate()
        {
            if (target == null || config == null) return;
            float dt = Time.deltaTime;
            if (attackedHoop == null)
            {
                Vector3 desiredPos = target.position + config.offset;
                transform.position = Vector3.Lerp(transform.position, desiredPos, Blend(config.followSmoothing, dt));
                transform.LookAt(target.position + Vector3.up * 1.2f);
                return;
            }

            Vector3 hoop = attackedHoop();
            Vector3 focus = target.position;
            Vector3 wanted = CameraRigMath.Direction(focus, hoop, courtCenter, config.aimAtHoop);
            // Turn around the vertical only (a slerp between opposite directions could swing
            // the camera over the top when the attack changes ends).
            float wantedYaw = Mathf.Atan2(wanted.x, wanted.z) * Mathf.Rad2Deg;
            yaw = placed ? Mathf.LerpAngle(yaw, wantedYaw, Blend(config.turnSmoothing, dt)) : wantedYaw;
            direction = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            CameraRigMath.Rig rig = CameraRigMath.Place(focus, hoop, direction, config);
            Quaternion look = Quaternion.LookRotation(rig.LookAt - rig.Position);
            if (!placed)
            {
                transform.SetPositionAndRotation(rig.Position, look);
                placed = true;
                return;
            }
            float follow = Blend(config.followSmoothing, dt);
            transform.position = Vector3.Lerp(transform.position, rig.Position, follow);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, follow);
        }

        private static float Blend(float rate, float dt) => 1f - Mathf.Exp(-rate * dt);

        internal void SetConfigForTest(CameraConfig testConfig)
        {
            config = testConfig;
        }
    }
}
