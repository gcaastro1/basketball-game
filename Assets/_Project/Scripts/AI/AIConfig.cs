using UnityEngine;

namespace Basket.AI
{
    [CreateAssetMenu(fileName = "AIConfig", menuName = "Basket/AI Config")]
    public class AIConfig : ScriptableObject
    {
        [Header("Defense")]
        // Closer than this to the ball handler -> contest instead of positional guard.
        public float contestDistance = 2.5f;
        // How close a contest closes in (keeps the two bodies from clipping).
        public float contestStandoff = 1.3f;
        // Positional guard: stand this far from the man, on the line to the hoop.
        public float guardDistance = 1.5f;

        [Header("Offense")]
        // Horizontal distance to the rim at which the AI stops driving and shoots.
        public float shootRange = 5.5f;
        // Minimum time holding the ball before a shot (no instant catch-and-fire loops).
        public float minHoldSecondsBeforeShot = 0.5f;

        [Header("Movement")]
        public float arrivalDistance = 0.2f;
        public bool sprintWhenChasing = true;
    }
}
