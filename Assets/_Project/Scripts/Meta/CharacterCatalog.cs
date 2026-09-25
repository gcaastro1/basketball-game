using System.Collections.Generic;
using UnityEngine;
using Basket.Characters;

namespace Basket.Meta
{
    // characterId -> CharacterDefinition em runtime. Não existia antes: BannerDefinition.pool
    // referencia personagens, mas só cobre quem está em algum banner ativo, não é fonte completa
    // (D-021).
    [CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Basket/Meta/Character Catalog")]
    public class CharacterCatalog : ScriptableObject
    {
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();

        public CharacterDefinition Find(string characterId)
        {
            foreach (var character in characters)
            {
                if (character != null && character.characterId == characterId) return character;
            }
            return null;
        }
    }
}
