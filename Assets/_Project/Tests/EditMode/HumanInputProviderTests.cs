using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using Basket.Core;
using Basket.Input;

public class HumanInputProviderTests : InputTestFixture
{
    private Keyboard keyboard;
    private HumanInputProvider provider;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        provider = new HumanInputProvider();
    }

    public override void TearDown()
    {
        provider.Dispose();
        base.TearDown();
    }

    private static MatchSnapshot Snapshot(int holder, float time)
    {
        var s = new MatchSnapshot(2);
        s.SetPlayer(0, TeamId.Home, Vector3.zero, Vector3.zero);
        s.SetPlayer(1, TeamId.Away, Vector3.one, Vector3.zero);
        s.SetBall(Vector3.zero, holder >= 0 ? BallState.Held : BallState.Free, holder);
        s.SetMatch(MatchPhase.Live, time);
        return s;
    }

    [Test]
    public void WPressed_MovesForward()
    {
        Press(keyboard.wKey);
        Assert.Greater(provider.Decide(null, 0).Move.y, 0f);
    }

    [Test]
    public void WithBall_SpaceHeld_IsShootHeld_NotJump()
    {
        Press(keyboard.spaceKey);
        var cmd = provider.Decide(Snapshot(holder: 0, time: 1f), 0);
        Assert.IsTrue(cmd.ShootHeld);
        Assert.IsFalse(cmd.Jump);

        Release(keyboard.spaceKey);
        Assert.IsFalse(provider.Decide(Snapshot(holder: 0, time: 1.1f), 0).ShootHeld, "letting go releases the shot");
    }

    [Test]
    public void WithoutBall_SpaceIsJump_EIsSteal()
    {
        // Both presses land in the same input update (same frame).
        Press(keyboard.spaceKey, queueEventOnly: true);
        Press(keyboard.eKey);
        var cmd = provider.Decide(Snapshot(holder: 1, time: 1f), 0);
        Assert.IsTrue(cmd.Jump);
        Assert.IsTrue(cmd.Steal);
        Assert.IsFalse(cmd.ShootHeld);
        Assert.IsFalse(cmd.Pass);
    }

    [Test]
    public void StealPress_DoesNotBecomeAPassAfterCatching()
    {
        Press(keyboard.eKey);
        Assert.IsTrue(provider.Decide(Snapshot(holder: -1, time: 1f), 0).Steal);
        InputSystem.Update(); // next frame

        // Caught the ball 0.05 s later, still inside the buffer window.
        Assert.IsFalse(provider.Decide(Snapshot(holder: 0, time: 1.05f), 0).Pass);
    }

    [Test]
    public void JumpPress_IsBufferedBriefly()
    {
        PressAndRelease(keyboard.spaceKey);
        Assert.IsTrue(provider.Decide(Snapshot(holder: -1, time: 2f), 0).Jump);
        InputSystem.Update(); // later frames: no new press
        Assert.IsTrue(provider.Decide(Snapshot(holder: -1, time: 2f + HumanInputProvider.BufferSeconds * 0.5f), 0).Jump);
        Assert.IsFalse(provider.Decide(Snapshot(holder: -1, time: 2f + HumanInputProvider.BufferSeconds * 2f), 0).Jump);
    }
}
