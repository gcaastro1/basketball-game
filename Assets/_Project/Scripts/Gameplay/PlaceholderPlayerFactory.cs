using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Capsule placeholder player. The root holds gameplay components (collision body,
    // motor, entity); visuals are children so a real character model can replace them
    // without changing the root's collision shape.
    public static class PlaceholderPlayerFactory
    {
        public const float BodyHeight = 1.9f;
        public const float BodyRadius = 0.35f;

        public static PlayerEntity Create(string name, TeamId team, PlayerMovementConfig movementConfig, Color bodyColor, Color markerColor)
        {
            var root = new GameObject(name);
            root.transform.position = Vector3.up * (BodyHeight * 0.5f);
            PhysicsLayers.Assign(root, PhysicsLayers.Player);

            var body = root.AddComponent<CharacterController>();
            body.height = BodyHeight;
            body.radius = BodyRadius;
            body.center = Vector3.zero;
            body.skinWidth = 0.03f;

            GameObject visual = PlaceholderArenaBuilder.CreateVisual(PrimitiveType.Capsule, "Body", root.transform, bodyColor, keepCollider: false);
            visual.transform.localScale = new Vector3(BodyRadius * 2f, BodyHeight * 0.5f, BodyRadius * 2f);

            // Shows which way the player faces.
            GameObject nose = PlaceholderArenaBuilder.CreateVisual(PrimitiveType.Cube, "Facing", root.transform, markerColor, keepCollider: false);
            nose.transform.localPosition = new Vector3(0f, 0.55f, BodyRadius);
            nose.transform.localScale = new Vector3(0.2f, 0.1f, 0.2f);

            var motor = root.AddComponent<PlayerMotor>();
            motor.Configure(movementConfig);
            var entity = root.AddComponent<PlayerEntity>();
            entity.Initialize(-1, team);
            return entity;
        }
    }
}
