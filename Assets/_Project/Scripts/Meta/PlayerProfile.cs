using System;
using System.Collections.Generic;
using Basket.Characters;

namespace Basket.Meta
{
    // The live player: inventory + economy + gacha memory + settings/story, built from a
    // PlayerSave and written back to one. Screens and the match flow talk to this.
    public sealed class PlayerProfile
    {
        public Inventory Inventory { get; }
        public EconomyService Economy { get; }
        public GachaSaveData Gacha { get; private set; } = new GachaSaveData();
        private PlayerSave extra = new PlayerSave();

        public PlayerProfile(ItemCatalog catalog = null)
        {
            Inventory = new Inventory(catalog != null ? id => catalog.Find(id)?.maxStack ?? 0 : (Func<string, long>)null);
            Economy = new EconomyService(Inventory);
        }

        public static PlayerProfile FromSave(PlayerSave save, ItemCatalog catalog = null)
        {
            var profile = new PlayerProfile(catalog);
            profile.Apply(save);
            return profile;
        }

        public void Apply(PlayerSave save)
        {
            Inventory.Clear();
            foreach (ItemStack s in save.items) Inventory.Add(s.itemId, s.amount);
            foreach (CharacterInstance c in save.characters) Inventory.AddCharacter(Copy(c));
            Gacha = save.gacha ?? new GachaSaveData();
            extra = save;
        }

        public PlayerSave ToSave()
        {
            var save = new PlayerSave
            {
                gacha = Gacha,
                settings = new List<SettingEntry>(extra.settings),
                storyChapter = extra.storyChapter,
                storyFlags = new List<string>(extra.storyFlags),
                matchInProgress = extra.matchInProgress,
            };
            foreach (var pair in Inventory.Items) save.items.Add(new ItemStack { itemId = pair.Key, amount = pair.Value });
            foreach (CharacterInstance c in Inventory.Characters) save.characters.Add(Copy(c));
            return save;
        }

        public string GetSetting(string key, string fallback = null) => extra.GetSetting(key, fallback);
        public void SetSetting(string key, string value) => extra.SetSetting(key, value);

        public GachaService CreateGacha(ProgressionConfig progression, CharacterObtainRules rules, Random rng = null) =>
            new GachaService(Economy, Inventory, progression, rules, Gacha, rng);

        private static CharacterInstance Copy(CharacterInstance c) =>
            new CharacterInstance(c.characterId, c.level, c.limitBreak, c.dupes) { xp = c.xp };
    }
}
