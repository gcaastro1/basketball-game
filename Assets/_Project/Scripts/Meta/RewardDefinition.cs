using UnityEngine;

namespace Basket.Meta
{
    // A named reward asset (story chapters, events, first-clear bonuses...).
    [CreateAssetMenu(fileName = "Reward", menuName = "Basket/Meta/Reward")]
    public class RewardDefinition : ScriptableObject
    {
        public string rewardId = "reward";
        public Reward reward = new Reward();
    }
}
