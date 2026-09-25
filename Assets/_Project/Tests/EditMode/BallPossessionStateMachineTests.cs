using NUnit.Framework;
using Basket.Core;
using Basket.Gameplay;

public class BallPossessionStateMachineTests
{
    [Test]
    public void InitialState_IsFree()
    {
        var sm = new BallPossessionStateMachine();
        Assert.AreEqual(BallState.Free, sm.CurrentState);
    }

    [Test]
    public void TryTransition_FreeToHeld_Succeeds()
    {
        var sm = new BallPossessionStateMachine();
        bool ok = sm.TryTransition(BallState.Held);
        Assert.IsTrue(ok);
        Assert.AreEqual(BallState.Held, sm.CurrentState);
    }

    [Test]
    public void TryTransition_FreeToFree_Fails()
    {
        var sm = new BallPossessionStateMachine();
        bool ok = sm.TryTransition(BallState.Free);
        Assert.IsFalse(ok);
    }

    [Test]
    public void TryTransition_HeldToShooting_Succeeds()
    {
        var sm = new BallPossessionStateMachine();
        sm.TryTransition(BallState.Held);
        bool ok = sm.TryTransition(BallState.Shooting);
        Assert.IsTrue(ok);
        Assert.AreEqual(BallState.Shooting, sm.CurrentState);
    }

    [Test]
    public void TryTransition_ValidChange_FiresOnStateChanged()
    {
        var sm = new BallPossessionStateMachine();
        BallState? observedFrom = null;
        BallState? observedTo = null;
        sm.OnStateChanged += (from, to) => { observedFrom = from; observedTo = to; };

        sm.TryTransition(BallState.Held);

        Assert.AreEqual(BallState.Free, observedFrom);
        Assert.AreEqual(BallState.Held, observedTo);
    }

    [Test]
    public void Passing_CanBeCaught()
    {
        var sm = new BallPossessionStateMachine();
        sm.TryTransition(BallState.Held);
        sm.TryTransition(BallState.Passing);
        Assert.IsTrue(sm.TryTransition(BallState.Held));
    }

    [Test]
    public void Shooting_CannotBeCaughtOutOfTheAir()
    {
        var sm = new BallPossessionStateMachine();
        sm.TryTransition(BallState.Held);
        sm.TryTransition(BallState.Shooting);
        Assert.IsFalse(sm.TryTransition(BallState.Held));
        Assert.IsTrue(sm.TryTransition(BallState.Free));
    }

    [Test]
    public void ForceState_BypassesTransitionTable()
    {
        var sm = new BallPossessionStateMachine();
        sm.TryTransition(BallState.Held);
        sm.TryTransition(BallState.Shooting);

        sm.ForceState(BallState.Held);

        Assert.AreEqual(BallState.Held, sm.CurrentState);
    }
}
