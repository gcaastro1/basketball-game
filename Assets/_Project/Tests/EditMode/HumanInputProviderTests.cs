using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Basket.Input;

public class HumanInputProviderTests : InputTestFixture
{
    private Keyboard keyboard;
    private HumanInputProvider provider;
    private GameObject go;

    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        go = new GameObject("TestInput");
        provider = go.AddComponent<HumanInputProvider>();
    }

    public override void TearDown()
    {
        Object.DestroyImmediate(go);
        base.TearDown();
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
}
