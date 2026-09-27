using UnityEngine;

namespace Basket.Gameplay
{
    // Geometry of the court. Real-world measurements in meters; the data assets use the NBA
    // court drawn on the stadium floor (docs/decisoes.md, D-026). Code defaults: FIBA half court.
    // When an authored arena replaces PlaceholderArenaBuilder, only the positions read by
    // gameplay (rim, check-ball spot) need to stay consistent with this asset.
    [CreateAssetMenu(fileName = "CourtConfig", menuName = "Basket/Court Config")]
    public class CourtConfig : ScriptableObject
    {
        [Header("Floor (x: -width/2..width/2, z: 0..depth)")]
        public float width = 15f;
        public float depth = 14f;
        // Full court: a second basket mirrored across the center line (z = depth / 2).
        public bool fullCourt = false;
        public float centerCircleRadius = 1.8f;
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
        // Inbound spot under the basket (distance from the point under the rim, toward the check spot).
        public float underBasketInboundDistance = 1.2f;
        // Free-throw line distance from the point under the rim (FIBA: 4.6 m from the backboard).
        public float freeThrowDistance = 4.2f;

        public Vector3 RimFloorProjection => new Vector3(rimCenter.x, 0f, rimCenter.z);
        public Vector3 CourtCenter => new Vector3(0f, 0f, depth * 0.5f);
        // Center of the whole court: this one's center, or the midcourt line (z = 0) of a half court.
        public Vector3 FullCourtCenter => fullCourt ? CourtCenter : Vector3.zero;

        // Mirror of a point across the center line (the other basket's side).
        public Vector3 Mirror(Vector3 p) => new Vector3(p.x, p.y, depth - p.z);
    }
}
