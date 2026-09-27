using System.Collections.Generic;
using UnityEngine;

namespace Basket.AI
{
    // Off-ball spacing spots, editable in the Inspector: (side, depth) in meters from the
    // point under the rim -- side to the right when facing the court's center from the rim,
    // depth toward the court's center. In priority order: the first ones are filled first
    // (the spots too close to the ball handler are skipped, OffensePlanner.AwayFrom).
    // A layout per team size; without one, the spots are computed on the arc (AIConfig).
    [CreateAssetMenu(fileName = "SpacingLayout", menuName = "Basket/AI Spacing Layout")]
    public class SpacingLayout : ScriptableObject
    {
        [Tooltip("Spots for a 3-player team (2 off the ball).")]
        public List<Vector2> threePlayers = new List<Vector2>
        {
            new Vector2(-5.4f, 5.4f), new Vector2(5.4f, 5.4f), new Vector2(0f, 7.8f),
            new Vector2(-7f, 0.9f), new Vector2(7f, 0.9f),
        };
        [Tooltip("Spots for a 5-player team (4 off the ball; with 4+ the best post player goes to the post).")]
        public List<Vector2> fivePlayers = new List<Vector2>
        {
            new Vector2(-7f, 0.9f), new Vector2(7f, 0.9f), new Vector2(-5.4f, 5.4f), new Vector2(5.4f, 5.4f),
            new Vector2(0f, 7.8f), new Vector2(-3.2f, 0.6f), new Vector2(3.2f, 0.6f),
        };

        public List<Vector2> For(int teamSize) => teamSize <= 3 ? threePlayers : fivePlayers;

        // World spots for this rim and court direction.
        public List<Vector3> Spots(Vector3 rimFloor, Vector3 courtAxis, int teamSize)
        {
            Vector3 axis = TeamMath.Flat(courtAxis);
            axis = axis.sqrMagnitude < 0.0001f ? Vector3.back : axis.normalized;
            Vector3 side = new Vector3(axis.z, 0f, -axis.x);
            var spots = new List<Vector3>();
            foreach (Vector2 s in For(teamSize)) spots.Add(TeamMath.Flat(rimFloor) + side * s.x + axis * s.y);
            return spots;
        }
    }
}
