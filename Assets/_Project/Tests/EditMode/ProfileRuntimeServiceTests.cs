using NUnit.Framework;
using UnityEngine;
using Basket.Core;
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

    [Test]
    public void BuildRosterOrNull_EnoughOwnedCharacters_ReturnsThem()
    {
        var storage = new MemorySaveStorage();
        var service = BuildService(storage);
        PlayerProfile profile = service.LoadOrCreate();
        var ace = ScriptableObject.CreateInstance<Basket.Characters.CharacterDefinition>();
        ace.characterId = "ace";
        // service's CharacterCatalog is private -- rebuild with a catalog containing "ace" for this test
        var catalogWithAce = ScriptableObject.CreateInstance<CharacterCatalog>();
        catalogWithAce.characters.Add(ace);
        var serviceWithCatalog = new ProfileRuntimeService(
            new SaveService(storage, new JsonSaveSerializer()),
            ScriptableObject.CreateInstance<ItemCatalog>(), catalogWithAce,
            ScriptableObject.CreateInstance<Basket.Characters.ProgressionConfig>(),
            ScriptableObject.CreateInstance<CharacterObtainRules>(),
            ScriptableObject.CreateInstance<MatchRewardRules>());
        serviceWithCatalog.LoadOrCreate();
        serviceWithCatalog.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace"));

        var roster = serviceWithCatalog.BuildRosterOrNull(requiredCount: 1);

        Assert.IsNotNull(roster);
        Assert.AreEqual(1, roster.Count);
        Assert.AreEqual(ace, roster[0]);
    }

    [Test]
    public void BuildRosterOrNull_NotEnoughOwnedCharacters_ReturnsNull()
    {
        var service = BuildService(new MemorySaveStorage());
        service.LoadOrCreate();

        var roster = service.BuildRosterOrNull(requiredCount: 1);

        Assert.IsNull(roster);
    }

    [Test]
    public void ApplyMatchReward_Win_GrantsXpOnlyToPlayedCharacters()
    {
        var storage = new MemorySaveStorage();
        var saveService = new SaveService(storage, new JsonSaveSerializer());
        var itemCatalog = ScriptableObject.CreateInstance<ItemCatalog>();
        var characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        var progression = ScriptableObject.CreateInstance<Basket.Characters.ProgressionConfig>();
        var obtainRules = ScriptableObject.CreateInstance<CharacterObtainRules>();
        // BuildService() usa um MatchRewardRules "zerado" (characterXp = 0 em win/loss/draw);
        // este teste precisa de xp > 0 na vitória, então monta o próprio MatchRewardRules
        // em vez de reusar o helper compartilhado.
        var rewardRules = ScriptableObject.CreateInstance<MatchRewardRules>();
        // 50 (não 100): com ProgressionConfig padrão (xpBase=100, xpExponent=1.5), 100 xp fecha
        // o nível 1 exatamente e AddXp zera o excedente -- o teste ficaria comparando 0 com 0.
        rewardRules.win.characterXp = 50;
        var service = new ProfileRuntimeService(saveService, itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
        service.LoadOrCreate();
        service.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace"));
        service.Profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("bench"));

        MatchRewardSummary summary = service.ApplyMatchReward(ownScore: 21, opponentScore: 15, playedCharacterIds: new[] { "ace" });

        Assert.IsTrue(summary.Won);
        Assert.Greater(service.Profile.Inventory.GetCharacter("ace").xp, 0);
        Assert.AreEqual(0, service.Profile.Inventory.GetCharacter("bench").xp);
    }
}
