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

        [Header("Hoops (around the gameplay rim, regulation size)")]
        [Tooltip("Net hung under each rim, scaled to the rim's diameter.")]
        public GameObject netModel;
        [Tooltip("Net length as a fraction of its width after scaling (0 = the model's own proportion).")]
        public float netLengthRatio = 0f;
        [Tooltip("Material of the backboard (glass).")]
        public Material backboardMaterial;
        public Color backboardLineColor = Color.white;
        [Tooltip("Padded base of the stanchion, on the floor behind each backboard.")]
        public GameObject stanchionBase;
        [Tooltip("Padded post of the stanchion, standing in the base.")]
        public GameObject stanchionPost;
        [Tooltip("Distance from the backboard to the center of the stanchion (m).")]
        public float stanchionDistance = 2.6f;
        public Color stanchionArmColor = new Color(0.12f, 0.12f, 0.14f);

        [Header("Ball")]
        [Tooltip("Model shown in place of the placeholder ball sphere.")]
        public GameObject ballModel;
        [Tooltip("The model is scaled to this diameter (m); keep it equal to the physics ball.")]
        public float ballDiameter = 0.24f;
    }
}
