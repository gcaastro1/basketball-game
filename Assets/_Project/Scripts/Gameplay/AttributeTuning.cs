using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // How much each attribute changes the game. Each pair is the multiplier at 0 and at
    // 99; at the neutral value (70) every multiplier is 1. Placeholder numbers for balancing.
    [CreateAssetMenu(fileName = "AttributeTuning", menuName = "Basket/Attribute Tuning")]
    public class AttributeTuning : ScriptableObject
    {
        [Header("Physical")]
        public Vector2 speed = new Vector2(0.85f, 1.12f);
        public Vector2 acceleration = new Vector2(0.75f, 1.2f);
        public Vector2 agilityTurn = new Vector2(0.7f, 1.2f);
        public Vector2 verticalJump = new Vector2(0.65f, 1.2f);
        // Speed while dribbling, by Ball Handling.
        public Vector2 handlingSpeed = new Vector2(0.88f, 1.03f);

        [Header("Shooting")]
        // Jump shots closer than this use Close Shot; up to the arc Mid Range; beyond, 3PT.
        public float closeShotMaxDistance = 4.5f;
        // Below this Dunk attribute a sprinting finish is a layup, not a dunk.
        public float minDunkAttribute = 55f;
        // Contest felt on finishes near the rim, by the better of Strength / Post Scoring.
        public Vector2 contactContest = new Vector2(1.3f, 0.6f);

        [Header("Defense")]
        // Contest a defender applies, by Perimeter (far) or Interior (near) Defense.
        public Vector2 defenderContest = new Vector2(0.6f, 1.3f);
        public Vector2 stealChance = new Vector2(0.4f, 1.6f);
        // Steal chance against a handler, by their Ball Handling.
        public Vector2 stealResistance = new Vector2(1.5f, 0.55f);
        public Vector2 blockRadius = new Vector2(0.75f, 1.25f);
        public Vector2 reboundRadius = new Vector2(0.8f, 1.25f);

        [Header("Passing")]
        // Pass apex height (lower = faster, flatter pass), by Passing.
        public Vector2 passApex = new Vector2(1.5f, 0.7f);

        [Header("Stamina (0..1 tank)")]
        public float sprintDrainPerSecond = 0.1f;
        public float jumpCost = 0.03f;
        public float recoveryPerSecond = 0.07f;
        // Drain multiplier by the Stamina attribute.
        public Vector2 staminaDrain = new Vector2(1.6f, 0.6f);
        // Below this, tiredness slows the player and hurts accuracy.
        public float tiredThreshold = 0.3f;
        public float tiredSpeedAtEmpty = 0.85f;
        public float tiredErrorAtEmpty = 1.3f;

        public static float Mult(IPlayerAttributes a, AttributeId id, Vector2 range) => Attributes.Centered(a, id, range.x, range.y);

        public float Tiredness(float stamina) =>
            stamina >= tiredThreshold ? 0f : 1f - Mathf.Clamp01(stamina / Mathf.Max(0.001f, tiredThreshold));

        public AttributeId ShotAttribute(ShotType type, float distance, float threePointRadius) => type switch
        {
            ShotType.Layup => AttributeId.Layup,
            ShotType.Dunk => AttributeId.Dunk,
            ShotType.FreeThrow => AttributeId.FreeThrow,
            _ => distance >= threePointRadius ? AttributeId.ThreePoint
                : distance <= closeShotMaxDistance ? AttributeId.CloseShot
                : AttributeId.MidRange
        };
    }
}
