using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.AI;
using Basket.Gameplay;

public class CharacterAITests
{
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 13.1f);

    private static MatchSnapshot TwoShooters(float threeOfPlayer0, float threeOfPlayer1)
    {
        var s = new MatchSnapshot(4);
        s.SetPlayer(0, TeamId.Home, new Vector3(-5f, 0f, 8f), Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(5f, 0f, 8f), Vector3.zero);
        s.SetPlayer(2, TeamId.Away, new Vector3(-5f, 0f, 3f), Vector3.zero);
        s.SetPlayer(3, TeamId.Away, new Vector3(5f, 0f, 3f), Vector3.zero);
        var a0 = AttributeSet.Uniform(70f);
        a0.Set(AttributeId.ThreePoint, threeOfPlayer0);
        var a1 = AttributeSet.Uniform(70f);
        a1.Set(AttributeId.ThreePoint, threeOfPlayer1);
        s.SetAttributes(0, a0);
        s.SetAttributes(1, a1);
        s.SetAttackingHoop(TeamId.Home, Rim);
        s.SetAttackingHoop(TeamId.Away, Rim);
        return s;
    }

    [Test]
    public void ShotValue_KnowsWhoTheShooterIs()
    {
        var config = ScriptableObject.CreateInstance<AIConfig>();
        var s = TwoShooters(threeOfPlayer0: 40f, threeOfPlayer1: 95f);
        Assert.Greater(TeamMath.ShotValue(s, 1, config), TeamMath.ShotValue(s, 0, config) * 1.3f);
    }

    [Test]
    public void ShotValue_NeutralPlayersAreUnchanged()
    {
        var config = ScriptableObject.CreateInstance<AIConfig>();
        var s = TwoShooters(70f, 70f);
        var neutral = TwoShooters(70f, 70f);
        neutral.SetAttributes(0, null);
        Assert.AreEqual(TeamMath.ShotValue(neutral, 0, config), TeamMath.ShotValue(s, 0, config), 1e-5f);
    }

    [Test]
    public void BestPassTarget_PrefersTheBetterShooter()
    {
        var config = ScriptableObject.CreateInstance<AIConfig>();
        var s = new MatchSnapshot(3);
        s.SetPlayer(0, TeamId.Home, new Vector3(0f, 0f, 5.5f), Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(-5f, 0f, 8f), Vector3.zero);
        s.SetPlayer(2, TeamId.Home, new Vector3(5f, 0f, 8f), Vector3.zero);
        var poor = AttributeSet.Uniform(70f);
        poor.Set(AttributeId.ThreePoint, 35f);
        var great = AttributeSet.Uniform(70f);
        great.Set(AttributeId.ThreePoint, 95f);
        s.SetAttributes(1, poor);
        s.SetAttributes(2, great);
        s.SetAttackingHoop(TeamId.Home, Rim);

        Assert.AreEqual(2, BallHandlerDecision.BestPassTarget(s, 0, config, out _));
    }

    [Test]
    public void AttributeTuning_PicksTheShotAttributeByZone()
    {
        var t = ScriptableObject.CreateInstance<AttributeTuning>();
        Assert.AreEqual(AttributeId.Layup, t.ShotAttribute(ShotType.Layup, 1.5f, 6.75f));
        Assert.AreEqual(AttributeId.Dunk, t.ShotAttribute(ShotType.Dunk, 1f, 6.75f));
        Assert.AreEqual(AttributeId.FreeThrow, t.ShotAttribute(ShotType.FreeThrow, 4.2f, 6.75f));
        Assert.AreEqual(AttributeId.CloseShot, t.ShotAttribute(ShotType.JumpShot, 3.5f, 6.75f));
        Assert.AreEqual(AttributeId.MidRange, t.ShotAttribute(ShotType.JumpShot, 5.5f, 6.75f));
        Assert.AreEqual(AttributeId.ThreePoint, t.ShotAttribute(ShotType.JumpShot, 7f, 6.75f));
    }

    [Test]
    public void AttributeTuning_Tiredness()
    {
        var t = ScriptableObject.CreateInstance<AttributeTuning>();
        Assert.AreEqual(0f, t.Tiredness(1f));
        Assert.AreEqual(0f, t.Tiredness(t.tiredThreshold));
        Assert.AreEqual(1f, t.Tiredness(0f));
    }
}
