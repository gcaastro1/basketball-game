using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Characters;
using Basket.Core;
using Basket.Meta;

// Task 11 (etapa 8.5): teste de aceite final do ciclo completo
// partida -> recompensa -> save -> gacha -> elenco. Ver docs/proximos-passos.md item A.
//
// Nota sobre o brief (task-11-brief.md): o esqueleto do Step 1 menciona `TestMatch`
// (helper de AISimulationTests.cs) na seção "Interfaces/Consumes", mas o código do
// próprio Step 1 nunca usa TestMatch/MatchManager -- ele chama
// service.ApplyMatchReward(ownScore, opponentScore, ...) diretamente, com placares fixos.
// Este teste segue o código exato do Step 1, sem simular uma partida real.
public class MetaIntegrationTests
{
    [UnityTest]
    public IEnumerator FullCycle_MatchEndsRewardsGrantedSaveSurvivesReload_GachaAddsToRoster()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "meta_integration_" + System.Guid.NewGuid());
        try
        {
            var itemCatalog = ScriptableObject.CreateInstance<ItemCatalog>();
            var characterCatalog = ScriptableObject.CreateInstance<CharacterCatalog>();
            var progression = ScriptableObject.CreateInstance<ProgressionConfig>();
            var obtainRules = ScriptableObject.CreateInstance<CharacterObtainRules>();
            var rewardRules = ScriptableObject.CreateInstance<MatchRewardRules>();

            // Divergência #1 do brief: MatchRewardRules.win é um Reward() em branco
            // (characterXp = 0 por padrão). RewardGranter.Grant só chama AddXp quando
            // reward.characterXp > 0 (Reward.cs linha 32), então sem configurar isso aqui
            // o XP nunca sobe e a asserção de XP abaixo falharia -- não por bug de
            // integração, mas porque o esqueleto do brief nunca preenche essa regra
            // (é dado provisório, D-021/regra do projeto: valores ficam em ScriptableObject).
            rewardRules.win.characterXp = 50;

            // Divergência #2 do brief: BuildRosterOrNull só enxerga personagens que
            // existem tanto no Inventory (CharacterInstance) quanto no CharacterCatalog
            // (CharacterDefinition) -- ProfileRuntimeService.cs linha 64 usa
            // characterCatalog.Find(instance.characterId). O esqueleto do brief adiciona
            // "ace" só como CharacterInstance no inventário, nunca como CharacterDefinition
            // no catálogo; sem isso "ace" nunca apareceria no elenco e o roster final
            // teria 1 item (só "rookie"), não 2. Registramos a definição aqui.
            var aceDefinition = ScriptableObject.CreateInstance<CharacterDefinition>();
            aceDefinition.characterId = "ace";
            characterCatalog.characters.Add(aceDefinition);

            var service = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(tempDir), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
            service.LoadOrCreate();
            service.Profile.Inventory.AddCharacter(new CharacterInstance("ace"));

            MatchRewardSummary rewardSummary = service.ApplyMatchReward(ownScore: 21, opponentScore: 10, playedCharacterIds: new[] { "ace" });
            Assert.IsTrue(rewardSummary.Won);

            // "fechar e abrir o jogo": novo serviço, mesmo diretório de disco.
            var reloadedService = new ProfileRuntimeService(
                new SaveService(new FileSaveStorage(tempDir), new JsonSaveSerializer()),
                itemCatalog, characterCatalog, progression, obtainRules, rewardRules);
            reloadedService.LoadOrCreate();
            Assert.IsTrue(reloadedService.Profile.Inventory.Owns("ace"), "save não sobreviveu ao fechar/reabrir simulado");
            Assert.Greater(reloadedService.Profile.Inventory.GetCharacter("ace").xp, 0, "XP da recompensa não persistiu");

            // gacha muda o elenco disponível
            var newCharacter = ScriptableObject.CreateInstance<CharacterDefinition>();
            newCharacter.characterId = "rookie";
            characterCatalog.characters.Add(newCharacter);
            var banner = ScriptableObject.CreateInstance<BannerDefinition>();
            banner.singleCost = new ItemCost("currency_gems", 0); // custo zero pra não depender de saldo aqui
            banner.rarities.Add(new GachaRarity { rarityId = "r1", rate = 1f });
            banner.pool.Add(new GachaEntry { character = newCharacter, rarityId = "r1", weight = 1f });

            Assert.IsNull(reloadedService.BuildRosterOrNull(requiredCount: 2), "não devia ter elenco suficiente ainda");
            GachaPullSummary pullSummary = reloadedService.Pull(banner, count: 1);
            Assert.IsTrue(pullSummary.Success);

            var roster = reloadedService.BuildRosterOrNull(requiredCount: 2);
            Assert.IsNotNull(roster, "gacha devia ter dado personagem suficiente pro elenco de 2");
            Assert.AreEqual(2, roster.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
        yield return null;
    }
}
