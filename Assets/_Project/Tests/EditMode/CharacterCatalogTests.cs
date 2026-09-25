using NUnit.Framework;
using UnityEngine;
using Basket.Characters;
using Basket.Meta;

public class CharacterCatalogTests
{
    [Test]
    public void Find_KnownId_ReturnsDefinition()
    {
        var ace = ScriptableObject.CreateInstance<CharacterDefinition>();
        ace.characterId = "ace";
        var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();
        catalog.characters.Add(ace);

        CharacterDefinition found = catalog.Find("ace");

        Assert.AreEqual(ace, found);
    }

    [Test]
    public void Find_UnknownId_ReturnsNull()
    {
        var catalog = ScriptableObject.CreateInstance<CharacterCatalog>();

        Assert.IsNull(catalog.Find("nobody"));
    }
}
