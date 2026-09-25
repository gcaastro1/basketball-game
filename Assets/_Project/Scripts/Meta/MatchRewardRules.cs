using UnityEngine;

namespace Basket.Meta
{
    // What a finished match pays (placeholders; the economy is undecided).
    [CreateAssetMenu(fileName = "MatchRewardRules", menuName = "Basket/Meta/Match Reward Rules")]
    public class MatchRewardRules : ScriptableObject
    {
        public Reward win = new Reward();
        public Reward loss = new Reward();
        public Reward draw = new Reward();

        public Reward For(int ownScore, int opponentScore) =>
            ownScore > opponentScore ? win : ownScore < opponentScore ? loss : draw;
    }
}
