using System;
using System.Collections.Generic;
using UnityEngine;
using Basket.Core;
using Basket.Characters;

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
            // Optional: null = neutral placeholder player.
            public CharacterDefinition character;
            // Exhibition progression for this slot (the save system provides real owned
            // characters in Etapa 8).
            public int level;
            public int limitBreak;
            public int dupes;

            public PlayerSlot(TeamId team, AgentControlType control, CharacterDefinition character = null, int level = 1, int limitBreak = 0, int dupes = 0)
            {
                this.team = team;
                this.control = control;
                this.character = character;
                this.level = level;
                this.limitBreak = limitBreak;
                this.dupes = dupes;
            }
        }

        public List<PlayerSlot> slots = new List<PlayerSlot>
        {
            new PlayerSlot(TeamId.Home, AgentControlType.Human),
            new PlayerSlot(TeamId.Away, AgentControlType.AI),
        };
    }
}
