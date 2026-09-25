using System.Collections.Generic;

namespace Basket.Core
{
    // Superfície somente-leitura do perfil do jogador, pra UI (que só conhece Core) ler sem
    // referenciar Basket.Meta. Implementada por PlayerProfile.
    public interface IPlayerProfileReadOnly
    {
        IEnumerable<KeyValuePair<string, long>> Items { get; }
        long Amount(string itemId);
        IEnumerable<string> OwnedCharacterIds { get; }
        bool OwnsCharacter(string characterId);
        int CharacterLevel(string characterId);
    }
}
