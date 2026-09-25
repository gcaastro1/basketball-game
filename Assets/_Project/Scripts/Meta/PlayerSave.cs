using System;
using System.Collections.Generic;
using Basket.Characters;

namespace Basket.Meta
{
    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public long amount;
    }

    [Serializable]
    public class SettingEntry
    {
        public string key;
        public string value;
    }

    // Player data only (brief section 28): configuration stays in ScriptableObjects and is
    // referenced here by id. Plain serializable fields -- JsonUtility-friendly.
    [Serializable]
    public class PlayerSave
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long savedUtcTicks;
        public List<ItemStack> items = new List<ItemStack>();
        public List<CharacterInstance> characters = new List<CharacterInstance>();
        public GachaSaveData gacha = new GachaSaveData();
        public List<SettingEntry> settings = new List<SettingEntry>();
        // Story progress (Etapa 9 fills these in).
        public string storyChapter = "";
        public List<string> storyFlags = new List<string>();
        // A match in progress, when one needs resuming (opaque to the save system).
        public string matchInProgress = "";

        public string GetSetting(string key, string fallback = null)
        {
            SettingEntry e = settings.Find(s => s.key == key);
            return e != null ? e.value : fallback;
        }

        public void SetSetting(string key, string value)
        {
            SettingEntry e = settings.Find(s => s.key == key);
            if (e == null) settings.Add(new SettingEntry { key = key, value = value });
            else e.value = value;
        }
    }

    public static class SaveMigrator
    {
        // version -> step that upgrades a save from that version to the next one.
        private static readonly Dictionary<int, Action<PlayerSave>> Steps = new Dictionary<int, Action<PlayerSave>>
        {
            // v0: saves written before versioning; lists may be missing.
            [0] = s =>
            {
                s.items ??= new List<ItemStack>();
                s.characters ??= new List<CharacterInstance>();
                s.gacha ??= new GachaSaveData();
                s.gacha.pity ??= new List<BannerPityState>();
                s.gacha.history ??= new List<GachaHistoryEntry>();
                s.settings ??= new List<SettingEntry>();
                s.storyFlags ??= new List<string>();
                s.storyChapter ??= "";
                s.matchInProgress ??= "";
            },
        };

        // Upgrades in place to CurrentVersion. False for a save from a newer build.
        public static bool Migrate(PlayerSave save)
        {
            if (save.version > PlayerSave.CurrentVersion) return false;
            while (save.version < PlayerSave.CurrentVersion)
            {
                if (Steps.TryGetValue(save.version, out Action<PlayerSave> step)) step(save);
                save.version++;
            }
            return true;
        }
    }
}
