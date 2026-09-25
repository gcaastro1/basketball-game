using System;
using System.Collections.Generic;

namespace Basket.Meta
{
    // Pity counters of one pity group (player data, saved).
    [Serializable]
    public class BannerPityState
    {
        public string group;
        public int pullsSinceTop;
        public bool featuredGuaranteed;
        public int totalPulls;
    }

    [Serializable]
    public class GachaHistoryEntry
    {
        public string bannerId;
        public string characterId;
        public string rarityId;
        public bool featured;
        public ObtainOutcome outcome;
        // Pulls since the last top rarity when this one was made (1 = right after one).
        public int pityCount;
        public long utcTicks;
    }

    // Everything the gacha remembers about the player.
    [Serializable]
    public class GachaSaveData
    {
        public List<BannerPityState> pity = new List<BannerPityState>();
        public List<GachaHistoryEntry> history = new List<GachaHistoryEntry>();

        public BannerPityState PityFor(string group)
        {
            BannerPityState s = pity.Find(p => p.group == group);
            if (s == null)
            {
                s = new BannerPityState { group = group };
                pity.Add(s);
            }
            return s;
        }
    }
}
