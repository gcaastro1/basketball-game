using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Basket/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        public Vector3 offset = new Vector3(0f, 6f, -8f);
        public float followSmoothing = 8f;
    }
}
