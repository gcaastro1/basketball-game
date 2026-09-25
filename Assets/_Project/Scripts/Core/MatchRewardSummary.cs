using System.Collections.Generic;

namespace Basket.Core
{
    // Resumo pronto-para-exibir do que uma partida deu. ProfileRuntimeService (Basket.Meta)
    // monta isso a partir dos tipos concretos dele (Reward, ObtainResult) antes de devolver --
    // a UI nunca vê os tipos de Meta.
    public readonly struct MatchRewardSummary
    {
        public readonly bool Won;
        public readonly IReadOnlyList<string> ItemsGranted;
        public readonly IReadOnlyList<string> CharactersObtained;

        public MatchRewardSummary(bool won, IReadOnlyList<string> itemsGranted, IReadOnlyList<string> charactersObtained)
        {
            Won = won;
            ItemsGranted = itemsGranted;
            CharactersObtained = charactersObtained;
        }
    }
}
