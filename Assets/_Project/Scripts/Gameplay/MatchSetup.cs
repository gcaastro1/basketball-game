using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;

namespace Basket.Gameplay
{
    // Who plays: one slot per player. 1v1, 3v3 and 5v5 are different assets, not
    // different code paths.
    [CreateAssetMenu(fileName = "MatchSetup", menuName = "Basket/Match Setup")]
    public class MatchSetup : ScriptableObject
    {
        [Serializable]
        public struct PlayerSlot
        {
            public TeamId team;
            public AgentControlType control;

            public PlayerSlot(TeamId team, AgentControlType control)
            {
                this.team = team;
                this.control = control;
            }
        }

        public List<PlayerSlot> slots = new List<PlayerSlot>
        {
            new PlayerSlot(TeamId.Home, AgentControlType.Human),
            new PlayerSlot(TeamId.Away, AgentControlType.AI),
        };
    }
}
