using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Basket.Characters;
using Basket.Meta;

// Etapa 8: gacha rates, pity, guarantees, costs, dupes and history -- all with seeded RNG.
public class GachaTests
{
    private static CharacterDefinition Char(string id)
    {
        var c = ScriptableObject.CreateInstance<CharacterDefinition>();
        c.characterId = id;
        return c;
    }

    // Top 3% / mid 17% / low 80%; hard pity 80, soft from 65 (+6%/pull), 50/50 rate-up with
    // guarantee, a ten-pull always holds a mid or better.
    private static BannerDefinition Banner(int hardPity = 80, int softStart = 65)
    {
        var b = ScriptableObject.CreateInstance<BannerDefinition>();
        b.bannerId = "test";
        b.rarities = new List<GachaRarity>
        {
            new GachaRarity { rarityId = "top", rate = 0.03f },
            new GachaRarity { rarityId = "mid", rate = 0.17f },
            new GachaRarity { rarityId = "low", rate = 0.80f },
        };
        b.pool = new List<GachaEntry>
        {
            new GachaEntry { character = Char("star"), rarityId = "top", featured = true },
            new GachaEntry { character = Char("veteran"), rarityId = "top" },
            new GachaEntry { character = Char("mid_a"), rarityId = "mid" },
            new GachaEntry { character = Char("mid_b"), rarityId = "mid" },
            new GachaEntry { character = Char("low_a"), rarityId = "low" },
            new GachaEntry { character = Char("low_b"), rarityId = "low" },
            new GachaEntry { character = Char("low_c"), rarityId = "low" },
        };
        b.pity = new GachaPity { hardPity = hardPity, softPityStart = softStart, softPityStep = 0.06f, featuredGuarantee = true, featuredChance = 0.5f };
        b.multiGuaranteeRarityId = "mid";
        b.singleCost = new ItemCost("gems", 160);
        b.multiCount = 10;
        b.multiCost = new ItemCost("gems", 1600);
        return b;
    }

    [Test]
    public void SampleBanner_IsValid()
    {
        Assert.IsEmpty(GachaEngine.Validate(Banner()));
    }

    [Test]
    public void Validate_CatchesBrokenBannerData()
    {
        BannerDefinition b = Banner();
        b.rarities[0].rate = 0.5f;
        b.pool.RemoveAll(e => e.rarityId == "mid");
        List<string> errors = GachaEngine.Validate(b);
        Assert.That(errors, Has.Some.Contains("sum"));
        Assert.That(errors, Has.Some.Contains("mid has no characters"));
    }

    [Test]
    public void WithoutPity_RatesConvergeToTheBannerData()
    {
        BannerDefinition b = Banner(hardPity: 0, softStart: 0);
        var state = new BannerPityState();
        var rng = new System.Random(1234);
        var counts = new int[3];
        const int n = 200000;
        for (int i = 0; i < n; i++) counts[GachaEngine.Roll(b, state, rng).RarityIndex]++;
        Assert.AreEqual(0.03, counts[0] / (double)n, 0.002);
        Assert.AreEqual(0.17, counts[1] / (double)n, 0.004);
        Assert.AreEqual(0.80, counts[2] / (double)n, 0.004);
    }

    [Test]
    public void HardPity_NeverLetsTheGapExceedIt()
    {
        BannerDefinition b = Banner(hardPity: 80, softStart: 0);
        var state = new BannerPityState();
        var rng = new System.Random(7);
        int gap = 0, maxGap = 0, hitsAtPity = 0;
        for (int i = 0; i < 100000; i++)
        {
            GachaRoll r = GachaEngine.Roll(b, state, rng);
            gap++;
            if (r.RarityIndex == 0)
            {
                maxGap = Mathf.Max(maxGap, gap);
                if (gap == 80) hitsAtPity++;
                gap = 0;
            }
        }
        Assert.AreEqual(80, maxGap, "never more than 80 pulls without the top rarity");
        Assert.Greater(hitsAtPity, 0, "and the 80th pull is a guaranteed hit");
    }

    [Test]
    public void SoftPity_RaisesTheChanceAfterItsStart()
    {
        BannerDefinition b = Banner();
        Assert.AreEqual(0.03f, GachaEngine.TopRate(b, 64), 1e-6f);
        Assert.AreEqual(0.09f, GachaEngine.TopRate(b, 65), 1e-6f);
        Assert.AreEqual(0.15f, GachaEngine.TopRate(b, 66), 1e-6f);
        Assert.AreEqual(1f, GachaEngine.TopRate(b, 80), 1e-6f);

        // Consolidated rate with pity is well above the base 3%.
        var state = new BannerPityState();
        var rng = new System.Random(99);
        int tops = 0;
        const int n = 100000;
        for (int i = 0; i < n; i++) if (GachaEngine.Roll(b, state, rng).RarityIndex == 0) tops++;
        Debug.Log($"Consolidated top rate with pity: {tops / (double)n:P2}");
        Assert.Greater(tops / (double)n, 0.03);
    }

    [Test]
    public void RatesExcludingTheTop_KeepTheirProportions()
    {
        float[] r = GachaEngine.Rates(Banner(), 70);
        Assert.AreEqual(1f, r[0] + r[1] + r[2], 1e-5f);
        Assert.AreEqual(0.17f / 0.80f, r[1] / r[2], 1e-4f);
    }

    [Test]
    public void LosingTheFiftyFifty_GuaranteesTheFeaturedNextTime()
    {
        BannerDefinition b = Banner();
        var state = new BannerPityState();
        var rng = new System.Random(3);
        bool lastTopWasLoss = false;
        int losses = 0, featured = 0;
        for (int i = 0; i < 100000; i++)
        {
            GachaRoll r = GachaEngine.Roll(b, state, rng);
            if (r.RarityIndex != 0) continue;
            if (lastTopWasLoss) Assert.IsTrue(r.Featured, "the top pull after a loss is the featured character");
            lastTopWasLoss = !r.Featured;
            if (r.Featured) featured++;
            else losses++;
        }
        Assert.Greater(losses, 0);
        Assert.Greater(featured, losses, "featured wins its 50% plus every guarantee");
    }

    private (Inventory, EconomyService, GachaService) Service(int seed, long gems)
    {
        var inventory = new Inventory();
        var economy = new EconomyService(inventory);
        inventory.Add("gems", gems);
        var service = new GachaService(economy, inventory, ScriptableObject.CreateInstance<ProgressionConfig>(),
            ScriptableObject.CreateInstance<CharacterObtainRules>(), new GachaSaveData(), new System.Random(seed), () => 42);
        return (inventory, economy, service);
    }

    [Test]
    public void MultiPull_AlwaysHoldsTheGuaranteedRarity_AndCostsTheMultiPrice()
    {
        BannerDefinition b = Banner();
        var (inventory, _, service) = Service(11, 1600 * 500);
        for (int i = 0; i < 500; i++)
        {
            PullOutcome o = service.Pull(b, 10);
            Assert.IsTrue(o.Success);
            Assert.AreEqual(10, o.Results.Count);
            Assert.IsTrue(o.Results.Exists(r => r.RarityId != "low"), "every ten-pull holds a mid or better");
        }
        Assert.AreEqual(0, inventory.Amount("gems"), "500 ten-pulls at the multi price");
    }

    [Test]
    public void CannotAfford_ChangesNothing()
    {
        BannerDefinition b = Banner();
        var (inventory, _, service) = Service(5, 159);
        PullOutcome o = service.Pull(b, 1);
        Assert.AreEqual(PullFailure.CannotAfford, o.Failure);
        Assert.AreEqual(159, inventory.Amount("gems"));
        Assert.IsEmpty(service.Data.history);
        Assert.IsEmpty(service.Data.pity);
        Assert.AreEqual(0, new List<CharacterInstance>(inventory.Characters).Count);
    }

    [Test]
    public void Pulls_GrantCharactersAndDupes_AndKeepHistoryAndPity()
    {
        BannerDefinition b = Banner();
        var (inventory, _, service) = Service(21, 160 * 300);
        int dupes = 0, news = 0;
        for (int i = 0; i < 300; i++)
        {
            PullOutcome o = service.Pull(b, 1);
            Assert.IsTrue(o.Success);
            if (o.Results[0].Outcome == ObtainOutcome.New) news++;
            else dupes++;
        }
        Assert.AreEqual(300, service.Data.history.Count);
        Assert.LessOrEqual(news, 7, "each character is new only once");
        Assert.Greater(dupes, 0);
        Assert.AreEqual(300, service.Data.PityFor("test").totalPulls);
        foreach (CharacterInstance c in inventory.Characters) Assert.LessOrEqual(c.dupes, 6);
        Assert.AreEqual(42, service.Data.history[0].utcTicks);
    }

    [Test]
    public void SameSeed_SameResults()
    {
        BannerDefinition b = Banner();
        var (_, _, first) = Service(77, 1600 * 5);
        var (_, _, second) = Service(77, 1600 * 5);
        for (int i = 0; i < 5; i++)
        {
            PullOutcome x = first.Pull(b, 10), y = second.Pull(b, 10);
            for (int k = 0; k < 10; k++) Assert.AreEqual(x.Results[k].CharacterId, y.Results[k].CharacterId);
        }
    }

    [Test]
    public void BannersSharingAPityGroup_ShareTheirCounters()
    {
        BannerDefinition a = Banner(), c = Banner();
        a.bannerId = "a";
        c.bannerId = "c";
        a.pity.group = c.pity.group = "standard";
        var (_, _, service) = Service(1, 160 * 20);
        for (int i = 0; i < 10; i++) service.Pull(a, 1);
        for (int i = 0; i < 10; i++) service.Pull(c, 1);
        Assert.AreEqual(1, service.Data.pity.Count);
        Assert.AreEqual(20, service.Data.PityFor("standard").totalPulls);
    }
}
