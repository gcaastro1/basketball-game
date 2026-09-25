using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Basket.Characters;
using Basket.Meta;

// Etapa 8: the shipped (placeholder) meta data is consistent -- every item id the data
// references exists, the banner is valid, and a real ten-pull works with it.
public class MetaAssetsTests
{
    private const string Folder = "Assets/_Project/Data/";

    private static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(Folder + path);
        Assert.IsNotNull(asset, path);
        return asset;
    }

    [Test]
    public void StandardBanner_IsValid()
    {
        Assert.IsEmpty(GachaEngine.Validate(Load<BannerDefinition>("Meta/StandardBanner.asset")));
    }

    [Test]
    public void EveryReferencedItem_IsInTheCatalog()
    {
        var catalog = Load<ItemCatalog>("Meta/ItemCatalog.asset");
        var referenced = new List<string>();
        foreach (LimitBreakStage stage in Load<ProgressionConfig>("DefaultProgressionConfig.asset").limitBreaks)
            foreach (ItemCost c in stage.costs) referenced.Add(c.itemId);
        foreach (ItemCost c in Load<CharacterObtainRules>("Meta/CharacterObtainRules.asset").maxedDupeConversion) referenced.Add(c.itemId);
        var match = Load<MatchRewardRules>("Meta/MatchRewardRules.asset");
        foreach (Reward r in new[] { match.win, match.loss, match.draw })
            foreach (ItemCost c in r.items) referenced.Add(c.itemId);
        var banner = Load<BannerDefinition>("Meta/StandardBanner.asset");
        referenced.Add(banner.singleCost.itemId);
        referenced.Add(banner.multiCost.itemId);

        foreach (string id in referenced) Assert.IsNotNull(catalog.Find(id), $"item '{id}' is referenced but not in the catalog");
    }

    [Test]
    public void ATenPull_WithTheShippedData_GivesTenCharacters()
    {
        var profile = new PlayerProfile(Load<ItemCatalog>("Meta/ItemCatalog.asset"));
        var banner = Load<BannerDefinition>("Meta/StandardBanner.asset");
        profile.Inventory.Add(banner.multiCost.itemId, banner.multiCost.count);
        GachaService gacha = profile.CreateGacha(Load<ProgressionConfig>("DefaultProgressionConfig.asset"),
            Load<CharacterObtainRules>("Meta/CharacterObtainRules.asset"), new System.Random(1));

        PullOutcome o = gacha.Pull(banner, banner.multiCount);
        Assert.IsTrue(o.Success, o.Failure.ToString());
        Assert.AreEqual(banner.multiCount, o.Results.Count);
        Assert.AreEqual(0, profile.Inventory.Amount(banner.multiCost.itemId));
        Assert.Greater(new List<CharacterInstance>(profile.Inventory.Characters).Count, 0);
    }
}
