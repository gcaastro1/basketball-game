using UnityEngine;

namespace Basket.Gameplay
{
    [CreateAssetMenu(fileName = "ShotConfig", menuName = "Basket/Shot Config")]
    public class ShotConfig : ScriptableObject
    {
        public float arcHeight = 3.5f;
        public float baseAccuracyRadius = 0.35f;
        public float defaultShooterRating = 0.75f;
    }
}
