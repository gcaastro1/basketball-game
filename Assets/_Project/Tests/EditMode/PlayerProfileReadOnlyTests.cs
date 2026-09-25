using System.Linq;
using NUnit.Framework;
using Basket.Core;
using Basket.Meta;

public class PlayerProfileReadOnlyTests
{
    [Test]
    public void PlayerProfile_ImplementsIPlayerProfileReadOnly_ReflectingRealState()
    {
        var profile = new PlayerProfile();
        profile.Inventory.Add("currency_gems", 100);
        var instance = profile.Inventory.AddCharacter(new Basket.Characters.CharacterInstance("ace", level: 5));

        IPlayerProfileReadOnly readOnly = profile;

        Assert.AreEqual(100, readOnly.Amount("currency_gems"));
        Assert.IsTrue(readOnly.OwnsCharacter("ace"));
        Assert.IsFalse(readOnly.OwnsCharacter("nobody"));
        Assert.AreEqual(5, readOnly.CharacterLevel("ace"));
        Assert.AreEqual(0, readOnly.CharacterLevel("nobody"));
        CollectionAssert.Contains(readOnly.OwnedCharacterIds.ToList(), "ace");
    }
}
