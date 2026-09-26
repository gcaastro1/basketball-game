using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

// Etapa 6.6 (D-026): the court data is the NBA court drawn on the stadium floor.
public class CourtDataTests
{
    private const string Data = "Assets/_Project/Data/";

    private static T Load<T>(string name) where T : Object
    {
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(Data + name);
        Assert.IsNotNull(asset, name);
        return asset;
    }

    [Test]
    public void FullCourt_IsNbaSized_RimsSixteenHundredFromTheBaseline()
    {
        var court = Load<CourtConfig>("Court5v5Config.asset");
        Assert.AreEqual(15.24f, court.width, 0.001f);
        Assert.AreEqual(28.65f, court.depth, 0.001f);
        Assert.AreEqual(1.6f, court.depth - court.rimCenter.z, 0.01f);
        Assert.AreEqual(3.05f, court.rimCenter.y, 0.001f);
        Assert.Greater(court.backboardCenter.z, court.rimCenter.z, "board behind the rim");
        Assert.AreEqual(new Vector3(0f, 0f, 14.325f), court.FullCourtCenter);
    }

    [Test]
    public void HalfCourt_IsHalfTheSameCourt_MidcourtAtZero()
    {
        var half = Load<CourtConfig>("DefaultCourtConfig.asset");
        var full = Load<CourtConfig>("Court5v5Config.asset");
        Assert.AreEqual(full.depth * 0.5f, half.depth, 0.001f);
        Assert.AreEqual(full.width, half.width, 0.001f);
        Assert.AreEqual(full.depth - full.rimCenter.z, half.depth - half.rimCenter.z, 0.001f);
        Assert.AreEqual(Vector3.zero, half.FullCourtCenter);
    }

    [TestCase("DefaultMatchRules.asset", "DefaultCourtConfig.asset")]
    [TestCase("FIBA3x3MatchRules.asset", "DefaultCourtConfig.asset")]
    [TestCase("FIBA5v5MatchRules.asset", "Court5v5Config.asset")]
    public void Rules_UseTheNbaLine_CheckBallIsBeyondIt(string rulesName, string courtName)
    {
        var rules = Load<MatchRules>(rulesName);
        var court = Load<CourtConfig>(courtName);
        Assert.AreEqual(7.24f, rules.threePointRadius, 0.001f);
        Assert.AreEqual(6.71f, rules.threePointCornerDistance, 0.001f);
        Assert.IsTrue(ScoringMath.IsBeyondArc(court.checkBallSpot, court.rimCenter, rules.threePointRadius, rules.threePointCornerDistance),
            "the check-ball spot is a three");
        Assert.Less(court.freeThrowDistance, rules.threePointRadius);
    }
}
