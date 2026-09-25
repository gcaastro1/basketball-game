using UnityEngine;
using Basket.Core;

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
