using UnityEngine;

namespace Basket.Presentation
{
    // Art for the arena, on top of the gameplay arena (PlaceholderArenaBuilder keeps every
    // collider: floor, walls, backboard, rim). Everything here is visual only.
    [CreateAssetMenu(fileName = "ArenaVisual", menuName = "Basket/Arena Visual")]
    public class ArenaVisualDefinition : ScriptableObject
    {
        [Tooltip("Stadium placed around the court, centered on the full court's center.")]
        public StadiumLayout stadium;
        [Tooltip("Hide the placeholder floor and lines (the stadium floor draws its own).")]
        public bool hidePlaceholderCourt = true;

        [Tooltip("Model shown in place of the placeholder ball sphere.")]
        public GameObject ballModel;
        [Tooltip("The model is scaled to this diameter (m); keep it equal to the physics ball.")]
        public float ballDiameter = 0.24f;
    }
}
