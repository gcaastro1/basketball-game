using NUnit.Framework;
using UnityEngine;
using Basket.Core;
using Basket.Gameplay;

public class MatchManagerOnMatchEndedTests
{
    [Test]
    public void OnMatchEnded_FiresOnceWhenPhaseBecomesEnded_WithFinalState()
    {
        var rules = ScriptableObject.CreateInstance<MatchRules>();
        rules.winningScore = 5;
        var manager = new MatchManager(rules, Vector3.zero);

        int firedCount = 0;
        MatchState received = null;
        manager.OnMatchEnded += state => { firedCount++; received = state; };

        // MatchState.EndWith is internal; Basket.Gameplay grants InternalsVisibleTo to
        // Basket.Tests.EditMode (see Assets/_Project/Scripts/Gameplay/AssemblyInfo.cs),
        // so it is callable directly from this test.
        manager.State.EndWith(TeamId.Home);

        Assert.AreEqual(1, firedCount);
        Assert.AreEqual(manager.State, received);
        Assert.AreEqual(MatchPhase.Ended, received.Phase);
    }

    [Test]
    public void OnMatchEnded_DoesNotFireForOtherPhaseChanges()
    {
        var rules = ScriptableObject.CreateInstance<MatchRules>();
        var manager = new MatchManager(rules, Vector3.zero);

        int firedCount = 0;
        manager.OnMatchEnded += _ => firedCount++;

        // Real path to a non-Ended phase change: BeginMatch() restarts possession and
        // calls MatchState.StartLivePlay(), which fires OnPhaseChanged(WaitingForInbound,
        // Live). MatchState.RegisterScore(TeamId, int) has no ShotType overload, and it
        // no-ops unless the match is already Live, so it would not exercise this path.
        manager.BeginMatch();

        Assert.AreEqual(MatchPhase.Live, manager.State.Phase);
        Assert.AreEqual(0, firedCount);
    }
}
