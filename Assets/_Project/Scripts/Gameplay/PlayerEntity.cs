using UnityEngine;
using Basket.Core;
using Basket.Characters;

namespace Basket.Gameplay
{
    // Identity of one player on the court: index in the roster, team, and the body
    // parts other systems need (motor, collider, feet). Controllers never touch this;
    // they only produce PlayerCommands.
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerEntity : MonoBehaviour
    {
        private CharacterController body;

        public int Index { get; private set; } = -1;
        public TeamId Team { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public Collider BodyCollider => body;

        // Character data (null = neutral placeholder player). With abilities, Attributes
        // already includes every active ability's modifiers.
        public IPlayerAttributes Attributes { get; private set; }
        public PlayerAbilities Abilities { get; private set; }
        public AITendencies Tendencies { get; private set; } = AITendencies.Neutral;
        public string CharacterName { get; private set; }
        public AttributeTuning Tuning { get; private set; }
        // 0..1 tank; see AttributeTuning stamina.
        public float Stamina { get; set; } = 1f;
        // In defensive guard this tick (Guard held without the ball): presentation shows the stance.
        public bool IsGuarding { get; set; }

        public void SetCharacter(string characterName, IPlayerAttributes attributes, PlayerAbilities abilities, AITendencies tendencies)
        {
            CharacterName = characterName;
            Abilities = abilities;
            Attributes = abilities != null ? abilities : attributes;
            Tendencies = tendencies;
        }

        public void SetTuning(AttributeTuning tuning) => Tuning = tuning;

        // Multiplier from an attribute (1 for a neutral player or without tuning).
        public float AttributeMult(AttributeId id, Vector2 range) => AttributeTuning.Mult(Attributes, id, range);

        private void Awake()
        {
            Motor = GetComponent<PlayerMotor>();
            body = GetComponent<CharacterController>();
        }

        public void Initialize(int index, TeamId team)
        {
            Index = index;
            Team = team;
        }

        // Fingertip height with arms raised (moves with the player while jumping).
        public float StandingReach => Motor != null && Motor.Config != null ? Motor.Config.standingReach : 2.4f;
        public Vector3 ReachPoint => FeetPosition + Vector3.up * StandingReach;

        public Vector3 FeetPosition => body != null
            ? transform.position + body.center + Vector3.down * (body.height * 0.5f)
            : transform.position;

        public void TeleportFeetTo(Vector3 feetPosition, Vector3 facing)
        {
            Vector3 pivotAboveFeet = transform.position - FeetPosition;
            facing.y = 0f;
            Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : transform.rotation;
            Motor.Teleport(feetPosition + pivotAboveFeet, rotation);
        }
    }
}
