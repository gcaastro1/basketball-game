using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Test]
    public void GetMoveInput_WPressed_ReturnsForward()
    {
        Press(keyboard.wKey);
        Vector2 input = provider.GetMoveInput();
        Assert.Greater(input.y, 0f);
    }

    [Test]
    public void WantsShoot_SpacePressedThisFrame_ReturnsTrue()
    {
        Press(keyboard.spaceKey);
        Assert.IsTrue(provider.WantsShoot());
    }

    [Test]
    public void Decide_WPressed_CommandMovesForward()
    {
        Press(keyboard.wKey);
        var command = provider.Decide(snapshot: null, selfIndex: 0);
        Assert.Greater(command.Move.y, 0f);
        Assert.IsFalse(command.Shoot);
    }
}
