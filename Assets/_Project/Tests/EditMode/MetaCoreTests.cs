using NUnit.Framework;
using UnityEngine;
using Basket.Characters;
using Basket.Meta;

// Etapa 8: inventory, economy, obtain rules, character upgrades, rewards.
public class MetaCoreTests
{
    private Inventory inventory;
    private EconomyService economy;
    private ProgressionConfig progression;
    private CharacterObtainRules rules;

    [SetUp]
    public void SetUp()
    {
        inventory = new Inventory();
        economy = new EconomyService(inventory);
        progression = ScriptableObject.CreateInstance<ProgressionConfig>();
        rules = ScriptableObject.CreateInstance<CharacterObtainRules>();
    }

    private static ItemCost Cost(string id, int n) => new ItemCost(id, n);

    [Test]
    public void Inventory_AddsAndNeverGoesNegative_AndRespectsStackCaps()
    {
        inventory.Add("coins", 50);
        inventory.Add("coins", -80);
        Assert.AreEqual(0, inventory.Amount("coins"));

        var capped = new Inventory(id => id == "gems" ? 100 : 0);
        capped.Add("gems", 250);
        Assert.AreEqual(100, capped.Amount("gems"));
    }

    [Test]
    public void Economy_SpendIsAllOrNothing()
    {
        inventory.Add("gems", 100);
        inventory.Add("coins", 5);
        Assert.IsFalse(economy.TrySpend(new[] { Cost("gems", 60), Cost("coins", 10) }, "test"));
        Assert.AreEqual(100, inventory.Amount("gems"), "nothing spent when one cost is short");

        // Two costs on the same item must both be covered.
        Assert.IsFalse(economy.CanAfford(new[] { Cost("gems", 60), Cost("gems", 60) }));
        Assert.IsTrue(economy.TrySpend(new[] { Cost("gems", 60), Cost("coins", 5) }, "test"));
        Assert.AreEqual(40, inventory.Amount("gems"));
        Assert.AreEqual(0, inventory.Amount("coins"));
    }

    [Test]
    public void Economy_ReportsEveryTransaction()
    {
        int seen = 0;
        economy.OnTransaction += t => seen++;
        economy.Grant(new[] { Cost("gems", 10), Cost("coins", 3) }, "gift");
        economy.TrySpend(new[] { Cost("gems", 4) }, "buy");
        Assert.AreEqual(3, seen);
    }

    [Test]
    public void Obtain_NewThenDupesUpToSixThenConverted()
    {
        Assert.AreEqual(ObtainOutcome.New, CharacterObtainer.Obtain("ace", inventory, economy, progression, rules, "t").Outcome);
        for (int i = 1; i <= 6; i++)
        {
            ObtainResult r = CharacterObtainer.Obtain("ace", inventory, economy, progression, rules, "t");
            Assert.AreEqual(ObtainOutcome.Dupe, r.Outcome);
            Assert.AreEqual(i, r.Instance.dupes);
        }
        ObtainResult extra = CharacterObtainer.Obtain("ace", inventory, economy, progression, rules, "t");
        Assert.AreEqual(ObtainOutcome.Converted, extra.Outcome, "base + 6 dupes is the maximum");
        Assert.AreEqual(1, inventory.Amount("dupe_overflow_token"));
    }

    [Test]
    public void XpItems_AreOnlySpentUpToTheLevelCap()
    {
        inventory.AddCharacter(new CharacterInstance("ace"));
        var chip = ScriptableObject.CreateInstance<ItemDefinition>();
        chip.itemId = "xp_chip";
        chip.kind = ItemKind.EvolutionItem;
        chip.xpValue = 1000;
        inventory.Add("xp_chip", 1000);

        long toCap = CharacterUpgrades.XpToCap(inventory.GetCharacter("ace"), progression);
        UpgradeResult r = CharacterUpgrades.UseXpItems(inventory, economy, progression, "ace", chip, 1000, out int used);
        Assert.AreEqual(UpgradeResult.Done, r);
        Assert.AreEqual((toCap + 999) / 1000, used, "only what the cap can absorb");
        Assert.AreEqual(progression.baseLevelCap, inventory.GetCharacter("ace").level);
        Assert.AreEqual(UpgradeResult.AtLevelCap, CharacterUpgrades.UseXpItems(inventory, economy, progression, "ace", chip, 1, out _));
    }

    [Test]
    public void LimitBreak_SpendsItsMaterialsFromTheInventory()
    {
        progression.limitBreaks[0].costs.Add(new ItemCost("limit_break_material_1", 3));
        inventory.AddCharacter(new CharacterInstance("ace", level: progression.limitBreaks[0].requiredLevel));
        Assert.AreEqual(UpgradeResult.MissingMaterials, CharacterUpgrades.LimitBreak(inventory, progression, "ace"));
        inventory.Add("limit_break_material_1", 5);
        Assert.AreEqual(UpgradeResult.Done, CharacterUpgrades.LimitBreak(inventory, progression, "ace"));
        Assert.AreEqual(1, inventory.GetCharacter("ace").limitBreak);
        Assert.AreEqual(2, inventory.Amount("limit_break_material_1"));
    }

    [Test]
    public void MatchRewards_PayByResult_AndGiveXpToTheCharactersWhoPlayed()
    {
        var match = ScriptableObject.CreateInstance<MatchRewardRules>();
        match.win.items.Add(Cost("coins", 100));
        match.win.characterXp = 50;
        match.loss.items.Add(Cost("coins", 40));
        inventory.AddCharacter(new CharacterInstance("ace"));
        inventory.AddCharacter(new CharacterInstance("bench"));

        RewardGranter.Grant(match.For(21, 15), inventory, economy, progression, rules, "match", new[] { "ace" });
        Assert.AreEqual(100, inventory.Amount("coins"));
        Assert.AreEqual(50, inventory.GetCharacter("ace").xp);
        Assert.AreEqual(0, inventory.GetCharacter("bench").xp, "only the characters who played");

        RewardGranter.Grant(match.For(10, 21), inventory, economy, progression, rules, "match");
        Assert.AreEqual(140, inventory.Amount("coins"));
    }
}
