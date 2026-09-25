using System.Collections.Generic;
using NUnit.Framework;
using Basket.Core;

public class CoreProfileTypesTests
{
    [Test]
    public void MatchRewardSummary_StoresAllFieldsExactly()
    {
        var items = new List<string> { "currency_gems x150" };
        var characters = new List<string> { "ace (New)" };

        var summary = new MatchRewardSummary(won: true, itemsGranted: items, charactersObtained: characters);

        Assert.IsTrue(summary.Won);
        Assert.AreEqual(items, summary.ItemsGranted);
        Assert.AreEqual(characters, summary.CharactersObtained);
    }

    [Test]
    public void GachaPullSummary_StoresAllFieldsExactly()
    {
        var results = new List<string> { "ace (Dupe)" };

        var summary = new GachaPullSummary(success: true, failureReason: null, resultDescriptions: results);

        Assert.IsTrue(summary.Success);
        Assert.IsNull(summary.FailureReason);
        Assert.AreEqual(results, summary.ResultDescriptions);
    }
}
