using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class PassTargetingTests
{
    [Test]
    public void NoTeammate_ReturnsMinusOne()
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Away, Vector3.right, Vector3.zero);

        Assert.AreEqual(-1, PassTargeting.SelectTarget(s, 0, Vector2.right));
    }

    [Test]
    public void WithAim_PicksMostAlignedTeammate()
    {
        var s = new MatchSnapshot(3);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(2f, 0f, 0f), Vector3.zero);
        s.SetPlayer(2, TeamId.Home, new Vector3(0f, 0f, 6f), Vector3.zero);

        Assert.AreEqual(2, PassTargeting.SelectTarget(s, 0, Vector2.up));
    }

    [Test]
    public void WithoutAim_PicksNearestTeammate()
    {
        var s = new MatchSnapshot(3);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Home, new Vector3(2f, 0f, 0f), Vector3.zero);
        s.SetPlayer(2, TeamId.Home, new Vector3(0f, 0f, 6f), Vector3.zero);

        Assert.AreEqual(1, PassTargeting.SelectTarget(s, 0, Vector2.zero));
    }
}
