using NUnit.Framework;
using UnityEngine;
using Basket.Core;

public class AIPerceptionTests
{
    [Test]
    public void Constructor_StoresAllFieldsExactly()
    {
        var self = new Vector3(1f, 0f, 2f);
        var opponent = new Vector3(3f, 0f, 4f);
        var ball = new Vector3(5f, 0f, 6f);

        var perception = new AIPerception(self, opponent, ball, opponentHasBall: true, selfHasBall: false);

        Assert.AreEqual(self, perception.SelfPosition);
        Assert.AreEqual(opponent, perception.OpponentPosition);
        Assert.AreEqual(ball, perception.BallPosition);
        Assert.IsTrue(perception.OpponentHasBall);
        Assert.IsFalse(perception.SelfHasBall);
    }

    private static MatchSnapshot ThreeVsOne(int holder)
    {
        // 0: Away (self), 1: Home near, 2: Home far, 3: Away teammate
        var s = new MatchSnapshot(4);
        s.SetPlayer(0, TeamId.Away, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(2f, 0f, 0f), Vector3.zero);
        s.SetPlayer(2, TeamId.Home, new Vector3(8f, 0f, 0f), Vector3.zero);
        s.SetPlayer(3, TeamId.Away, new Vector3(0f, 0f, 5f), Vector3.zero);
        s.SetAttackingHoop(TeamId.Home, new Vector3(0f, 3f, 13f));
        s.SetAttackingHoop(TeamId.Away, new Vector3(0f, 3f, 13f));
        s.SetBall(Vector3.one, BallState.Held, holder);
        s.SetMatch(MatchPhase.Live, 7f);
        return s;
    }

    [Test]
    public void FromSnapshot_OpponentHolder_FocusesOnHolderNotNearest()
    {
        var p = AIPerception.FromSnapshot(ThreeVsOne(holder: 2), selfIndex: 0);

        Assert.IsTrue(p.OpponentHasBall);
        Assert.IsFalse(p.SelfHasBall);
        Assert.IsFalse(p.TeammateHasBall);
        Assert.AreEqual(new Vector3(8f, 0f, 0f), p.OpponentPosition);
        Assert.AreEqual(7f, p.Time);
    }

    [Test]
    public void FromSnapshot_TeammateHolder_FocusesOnNearestOpponent()
    {
        var p = AIPerception.FromSnapshot(ThreeVsOne(holder: 3), selfIndex: 0);

        Assert.IsTrue(p.TeammateHasBall);
        Assert.IsFalse(p.OpponentHasBall);
        Assert.AreEqual(new Vector3(2f, 0f, 0f), p.OpponentPosition);
    }

    [Test]
    public void FromSnapshot_SelfHolder_SetsSelfHasBallAndHoops()
    {
        var p = AIPerception.FromSnapshot(ThreeVsOne(holder: 0), selfIndex: 0);

        Assert.IsTrue(p.SelfHasBall);
        Assert.AreEqual(new Vector3(0f, 3f, 13f), p.AttackHoop);
        Assert.AreEqual(new Vector3(0f, 3f, 13f), p.DefendHoop);
    }
}
