using System;
using Basket.Characters;

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
    }
}
