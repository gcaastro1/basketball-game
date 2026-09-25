using NUnit.Framework;
using UnityEngine;
using Basket.Core;

public class MatchSnapshotTests
{
    [Test]
    public void FindNearestOpponent_IgnoresTeammatesAndHeight()
    {
        var s = new MatchSnapshot(3);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(0.5f, 0f, 0f), Vector3.zero);
        s.SetPlayer(2, TeamId.Away, new Vector3(3f, 5f, 0f), Vector3.zero);

        Assert.AreEqual(2, s.FindNearestOpponent(0));
    }

    [Test]
    public void FindNearestOpponent_NoOpponents_ReturnsMinusOne()
    {
        var s = new MatchSnapshot(1);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        Assert.AreEqual(-1, s.FindNearestOpponent(0));
    }

    [Test]
    public void TeamInPossession_FollowsHolder()
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Away, Vector3.one, Vector3.zero);

        s.SetBall(Vector3.zero, BallState.Free, -1);
        Assert.IsNull(s.TeamInPossession);

        s.SetBall(Vector3.zero, BallState.Held, 1);
        Assert.AreEqual(TeamId.Away, s.TeamInPossession);
    }

    [Test]
    public void IsClosestOfTeamToBall_ComparesOnlyTeammates()
    {
        var s = new MatchSnapshot(3);
        s.SetPlayer(0, TeamId.Home, new Vector3(2f, 0f, 0f), Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(5f, 0f, 0f), Vector3.zero);
        s.SetPlayer(2, TeamId.Away, new Vector3(0.5f, 0f, 0f), Vector3.zero);
        s.SetBall(Vector3.zero, BallState.Free, -1);

        Assert.IsTrue(s.IsClosestOfTeamToBall(0));
        Assert.IsFalse(s.IsClosestOfTeamToBall(1));
        Assert.IsTrue(s.IsClosestOfTeamToBall(2));
    }
}
