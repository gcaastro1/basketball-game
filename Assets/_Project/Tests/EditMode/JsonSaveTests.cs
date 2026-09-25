using NUnit.Framework;
using Basket.Characters;
using Basket.Meta;

// Etapa 8: the real serializer (Unity's JsonUtility) round-trips a full save.
public class JsonSaveTests
{
    [Test]
    public void JsonSave_RoundTripsEverything()
    {
        var profile = new PlayerProfile();
        profile.Inventory.Add("currency_gems", 1600);
        profile.Inventory.AddCharacter(new CharacterInstance("sample_sharpshooter", level: 20, limitBreak: 1, dupes: 3) { xp = 7 });
        profile.Gacha.PityFor("standard").pullsSinceTop = 12;
        profile.Gacha.history.Add(new GachaHistoryEntry { bannerId = "standard", characterId = "sample_sharpshooter", rarityId = "rarity_3", outcome = ObtainOutcome.Dupe, pityCount = 12 });
        profile.SetSetting("language", "pt-BR");

        var storage = new MemorySaveStorage();
        var service = new SaveService(storage, new JsonSaveSerializer());
        service.Save("main", profile.ToSave());
        StringAssert.Contains("sample_sharpshooter", storage.Main, "human-readable JSON");

        SaveLoadResult r = service.Load("main");
        Assert.AreEqual(SaveLoadStatus.Loaded, r.Status);
        PlayerProfile back = PlayerProfile.FromSave(r.Save);
        Assert.AreEqual(1600, back.Inventory.Amount("currency_gems"));
        CharacterInstance c = back.Inventory.GetCharacter("sample_sharpshooter");
        Assert.AreEqual(20, c.level);
        Assert.AreEqual(1, c.limitBreak);
        Assert.AreEqual(3, c.dupes);
        Assert.AreEqual(7, c.xp);
        Assert.AreEqual(12, back.Gacha.PityFor("standard").pullsSinceTop);
        Assert.AreEqual(ObtainOutcome.Dupe, back.Gacha.history[0].outcome);
        Assert.AreEqual("pt-BR", back.GetSetting("language"));
    }
}
