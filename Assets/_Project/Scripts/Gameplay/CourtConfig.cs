using UnityEngine;

namespace Basket.Gameplay
{
    // Geometry of the (placeholder) half court. Real-world measurements (FIBA) in meters.
    // When an authored arena replaces PlaceholderArenaBuilder, only the positions read by
    // gameplay (rim, check-ball spot) need to stay consistent with this asset.
    [CreateAssetMenu(fileName = "CourtConfig", menuName = "Basket/Court Config")]
    public class CourtConfig : ScriptableObject
    {
        [Header("Floor (x: -width/2..width/2, z: 0..depth)")]
        public float width = 15f;
        public float depth = 14f;
        public float wallHeight = 3f;

        [Header("Hoop")]
        public Vector3 rimCenter = new Vector3(0f, 3.05f, 13.1f);
        public float rimRadius = 0.2286f;
        public float rimTubeRadius = 0.01f;
        public int rimSegments = 16;
        public Vector3 backboardCenter = new Vector3(0f, 3.425f, 13.5f);
        public Vector3 backboardSize = new Vector3(1.8f, 1.05f, 0.05f);

        [Header("Possession restart")]
        public Vector3 checkBallSpot = new Vector3(0f, 0f, 5.5f);
        public float defenderGap = 1.5f;
        public float supportPlayerSpreadDegrees = 45f;

        public Vector3 RimFloorProjection => new Vector3(rimCenter.x, 0f, rimCenter.z);
    }
}
