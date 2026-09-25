using NUnit.Framework;
using UnityEngine;
using Basket.Meta;

public class ProfileRuntimeServiceTests
{
    private ProfileRuntimeService BuildService(MemorySaveStorage storage)
    {
        var saveService = new SaveService(storage, new JsonSaveSerializer());
        var itemCatalog = ScriptableObject.CreateInstance<ItemCatalog>();
        var characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        var progression = ScriptableObject.CreateInstance<Basket.Characters.ProgressionConfig>();
        var obtainRules = ScriptableObject.CreateInstance<CharacterObtainRules>();
        var rewardRules = ScriptableObject.CreateInstance<MatchRewardRules>();
        return new ProfileRuntimeService(saveService, itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
    }

    [Test]
    public void LoadOrCreate_NoExistingSave_CreatesNewProfile()
    {
        var service = BuildService(new MemorySaveStorage());

        PlayerProfile profile = service.LoadOrCreate();

        Assert.IsNotNull(profile);
        Assert.AreEqual(SaveLoadStatus.NewPlayer, service.LastLoadStatus);
    }

    [Test]
    public void Save_ThenLoadOrCreate_RoundTripsCharacters()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        PlayerProfile profile = service.LoadOrCreate();
        profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace", level: 3));

        service.Save();

        var reloadService = BuildService(storage); // same backing storage, fresh service (simulates reopening the game)
        PlayerProfile reloaded = reloadService.LoadOrCreate();
        Assert.IsTrue(reloaded.Inventory.Owns("ace"));
        Assert.AreEqual(3, reloaded.Inventory.GetCharacter("ace").level);
    }
}
