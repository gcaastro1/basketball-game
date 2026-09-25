using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class PlayerMotorTests
{
    [UnityTest]
    public IEnumerator Tick_WithForwardInput_MovesPlayerForwardOverTime()
    {
        var go = new GameObject("TestPlayer");
        go.AddComponent<CharacterController>();
        var motor = go.AddComponent<PlayerMotor>();

        var config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        motor.SetConfigForTest(config);

        Vector3 startPos = go.transform.position;

        for (int i = 0; i < 30; i++)
        {
            motor.Tick(new Vector2(0f, 1f), sprint: false, dt: 0.02f);
            yield return null;
        }

        Assert.Greater(Vector3.Distance(go.transform.position, startPos), 0.01f);
        Object.Destroy(go);
    }
}
