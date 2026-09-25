using System;
using System.Collections.Generic;
using System.Linq;
using Basket.Characters;
using Basket.Core;

namespace Basket.Meta
{
    // Coordenador de perfil em runtime -- POCO, sem MonoBehaviour, mesmo molde de SaveService/
    // EconomyService (D-021). GameBootstrap só instancia e chama nos pontos certos.
    public sealed class ProfileRuntimeService
    {
        private const string SaveSlot = "profile";

        private readonly SaveService saveService;
        private readonly ItemCatalog itemCatalog;
        private readonly CharacterCatalog characterCatalog;
        private readonly ProgressionConfig progressionConfig;
        private readonly CharacterObtainRules obtainRules;
        private readonly MatchRewardRules rewardRules;
        private readonly Random rng;

        public PlayerProfile Profile { get; private set; }
        public SaveLoadStatus LastLoadStatus { get; private set; }

        public ProfileRuntimeService(SaveService saveService, ItemCatalog itemCatalog, CharacterCatalog characterCatalog,
            ProgressionConfig progressionConfig, CharacterObtainRules obtainRules, MatchRewardRules rewardRules, Random rng = null)
        {
            this.saveService = saveService;
            this.itemCatalog = itemCatalog;
            this.characterCatalog = characterCatalog;
            this.progressionConfig = progressionConfig;
            this.obtainRules = obtainRules;
            this.rewardRules = rewardRules;
            this.rng = rng ?? new Random();
        }

        public PlayerProfile LoadOrCreate()
        {
            SaveLoadResult result = saveService.Load(SaveSlot);
            LastLoadStatus = result.Status;
            // NewerVersion: não sobrescreve o save mais novo. Joga com um perfil novo em memória
            // esta sessão em vez de arriscar perder dados de uma build futura (Save() abaixo já
            // recusa gravar nesse caso).
            Profile = result.Status == SaveLoadStatus.NewerVersion
                ? new PlayerProfile(itemCatalog)
                : PlayerProfile.FromSave(result.Save, itemCatalog);
            return Profile;
        }

        public void Save()
        {
            if (Profile == null || LastLoadStatus == SaveLoadStatus.NewerVersion) return;
            saveService.Save(SaveSlot, Profile.ToSave());
        }

        // Elenco possível a partir do que o jogador possui de verdade. Devolve null (o chamador
        // cai para o MatchSetup configurado) se o perfil não tiver personagens suficientes ainda.
        public List<CharacterDefinition> BuildRosterOrNull(int requiredCount)
        {
            var owned = new List<CharacterDefinition>();
            foreach (CharacterInstance instance in Profile.Inventory.Characters)
            {
                CharacterDefinition definition = characterCatalog.Find(instance.characterId);
                if (definition != null) owned.Add(definition);
            }
            return owned.Count >= requiredCount ? owned : null;
        }

        // Aplica a recompensa da partida (itens, personagens, xp aos que jogaram) e devolve um
        // resumo já em tipos de Basket.Core (a UI nunca vê Reward/ObtainResult).
        public MatchRewardSummary ApplyMatchReward(int ownScore, int opponentScore, IEnumerable<string> playedCharacterIds)
        {
            Reward reward = rewardRules.For(ownScore, opponentScore);
            List<ObtainResult> obtainResults = RewardGranter.Grant(reward, Profile.Inventory, Profile.Economy,
                progressionConfig, obtainRules, reason: "match_result", xpTo: playedCharacterIds);
            Save();

            List<string> itemDescriptions = reward.items?.Select(cost => $"{cost.itemId} x{cost.count}").ToList()
                ?? new List<string>();
            List<string> characterDescriptions = obtainResults.Select(r => $"{r.Instance.characterId} ({r.Outcome})").ToList();

            return new MatchRewardSummary(won: ownScore > opponentScore, itemsGranted: itemDescriptions, charactersObtained: characterDescriptions);
        }
    }
}
