using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "Basket/Ball Config")]
    public class BallConfig : ScriptableObject
    {
        public float mass = 0.62f;
        public float drag = 0.05f;
        public float angularDrag = 0.3f;
        public float handHeightOffset = 1.1f;
        public float bounciness = 0.75f;
        public float catchRadius = 1.0f;
    }
}
