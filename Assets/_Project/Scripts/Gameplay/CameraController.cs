using UnityEngine;

namespace Basket.Gameplay
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private CameraConfig config;
        private Transform target;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        public void Configure(Transform followTarget, CameraConfig cameraConfig)
        {
            config = cameraConfig;
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desiredPos = target.position + config.offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, 1f - Mathf.Exp(-config.followSmoothing * Time.deltaTime));
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }

        internal void SetConfigForTest(CameraConfig testConfig)
        {
            config = testConfig;
        }
    }
}
