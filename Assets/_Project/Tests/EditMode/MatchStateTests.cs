using NUnit.Framework;
using Basket.Core;
using Basket.Gameplay;

public class MatchStateTests
{
    [Test]
    public void RegisterScore_WhileWaitingForInbound_IsIgnored()
    {
        var state = new MatchState();
        state.RegisterScore(TeamId.Home, 2);
        Assert.AreEqual(0, state.ScoreHome);
    }

    [Test]
    public void RegisterScore_WhileLive_AddsPointsToThatTeamAndTransitionsToScored()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(TeamId.Away, 3);

        Assert.AreEqual(0, state.ScoreHome);
        Assert.AreEqual(3, state.ScoreAway);
        Assert.AreEqual(3, state.GetScore(TeamId.Away));
        Assert.AreEqual(MatchPhase.Scored, state.Phase);
    }

    [Test]
    public void ResetForNextPossession_ReturnsToWaitingForInbound()
    {
        var state = new MatchState();
        state.StartLivePlay();
        state.RegisterScore(TeamId.Home, 2);
        state.ResetForNextPossession();

        Assert.AreEqual(MatchPhase.WaitingForInbound, state.Phase);
    }

    [Test]
    public void RegisterScore_ReachingWinningScore_EndsMatchWithWinner()
    {
        var state = new MatchState(winningScore: 4);
        state.StartLivePlay();
        state.RegisterScore(TeamId.Home, 3);
        state.ResetForNextPossession();
        state.StartLivePlay();
        state.RegisterScore(TeamId.Home, 2);

        Assert.AreEqual(MatchPhase.Ended, state.Phase);
        Assert.AreEqual(TeamId.Home, state.Winner);
    }

    [Test]
    public void Ended_IgnoresFurtherPhaseChanges()
    {
        var state = new MatchState(winningScore: 2);
        state.StartLivePlay();
        state.RegisterScore(TeamId.Away, 2);

        state.ResetForNextPossession();
        state.StartLivePlay();

        Assert.AreEqual(MatchPhase.Ended, state.Phase);
    }

    [Test]
    public void ZeroWinningScore_NeverEnds()
    {
        var state = new MatchState(winningScore: 0);
        state.StartLivePlay();
        state.RegisterScore(TeamId.Home, 100);

        Assert.AreEqual(MatchPhase.Scored, state.Phase);
        Assert.IsNull(state.Winner);
    }
}
