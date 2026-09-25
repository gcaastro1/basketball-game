using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Scoring/flow rules for one match mode. 3v3 and 5v5 get their own assets; values
    // here are PROVISIONAL defaults (see docs/decisoes.md, D-004).
    [CreateAssetMenu(fileName = "MatchRules", menuName = "Basket/Match Rules")]
    public class MatchRules : ScriptableObject
    {
        public int pointsInsideArc = 2;
        public int pointsBeyondArc = 3;
        public float threePointRadius = 6.75f;
        // 0 = no score limit.
        public int winningScore = 21;
        public float restartDelaySeconds = 1.5f;
        public TeamId firstPossession = TeamId.Home;
    }
}
