using NUnit.Framework;
using Basket.Core;
using Basket.Gameplay;

public class MatchStateTests
{
    [Test]
    public void RegisterScore_WhileWaitingForInbound_IsIgnored()
    {
        var state = new MatchState();
        state.RegisterScore(homeScored: true, points: 2);
        Assert.AreEqual(0, state.ScoreHome);
    }

    [Test]
    public void RegisterScore_WhileLive_AddsPointsAndTransitionsToScored()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(homeScored: true, points: 2);

        Assert.AreEqual(2, state.ScoreHome);
        Assert.AreEqual(MatchPhase.Scored, state.Phase);
    }

    [Test]
    public void ResetForNextPossession_ReturnsToWaitingForInbound()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(true, 2);
        state.ResetForNextPossession();

        Assert.AreEqual(MatchPhase.WaitingForInbound, state.Phase);
    }
}
