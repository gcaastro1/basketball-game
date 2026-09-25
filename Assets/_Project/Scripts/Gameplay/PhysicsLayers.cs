using UnityEngine;

namespace Basket.Gameplay
{
    // Names must match ProjectSettings/TagManager.asset. Gameplay logic does not depend on
    // layers (it uses components); layers exist for the collision matrix and raycasts.
    public static class PhysicsLayers
    {
        public const string Player = "Player";
        public const string Ball = "Ball";
        public const string Court = "Court";
        public const string Hoop = "Hoop";

        public static void Assign(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) go.layer = layer;
        }
    }
}
