using UnityEngine;
using Basket.Core;

namespace Basket.UI
{
    // Placeholder (Etapa 10 faz a UI final) -- overlay IMGUI mostrando saldo/elenco e o último
    // resultado de partida/gacha. Só conhece Core (D-021): MatchRewardSummary/GachaPullSummary
    // já vêm prontos pra exibir, sem tipo nenhum de Basket.Meta cruzando essa fronteira.
    public class ProfileHud : MonoBehaviour
    {
        private IPlayerProfileReadOnly profile;
        private MatchRewardSummary? lastReward;
        private GachaPullSummary? lastPull;

        public void Configure(IPlayerProfileReadOnly playerProfile)
        {
            profile = playerProfile;
        }

        public void ShowMatchReward(MatchRewardSummary summary) => lastReward = summary;
        public void ShowGachaResult(GachaPullSummary summary) => lastPull = summary;

        private void OnGUI()
        {
            if (profile == null) return;

            GUI.Label(new Rect(10, 70, 400, 20), $"Gemas: {profile.Amount("currency_gems")}");
            GUI.Label(new Rect(10, 90, 400, 20), $"Personagens: {System.Linq.Enumerable.Count(profile.OwnedCharacterIds)}");

            int y = 110;
            if (lastReward.HasValue)
            {
                var r = lastReward.Value;
                GUI.Label(new Rect(10, y, 500, 20), $"Última partida: {(r.Won ? "Vitória" : "Derrota")} — {string.Join(", ", r.ItemsGranted)}");
                y += 20;
            }
            if (lastPull.HasValue)
            {
                var p = lastPull.Value;
                GUI.Label(new Rect(10, y, 500, 20), p.Success
                    ? $"Gacha: {string.Join(", ", p.ResultDescriptions)}"
                    : $"Gacha falhou: {p.FailureReason}");
            }
        }
    }
}
