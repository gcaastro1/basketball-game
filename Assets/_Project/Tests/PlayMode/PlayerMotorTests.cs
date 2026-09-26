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

    [UnityTest]
    public IEnumerator Jump_ReachesConfiguredHeightAndLands()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        var config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        PlayerEntity player = PlaceholderPlayerFactory.Create("Jumper", Basket.Core.TeamId.Home, config, Color.gray, Color.yellow);
        yield return null;

        for (int i = 0; i < 20 && !player.Motor.IsGrounded; i++) player.Motor.Tick(Vector2.zero, false, 0.02f);
        Assert.IsTrue(player.Motor.IsGrounded, "settles on the floor");
        float groundY = player.FeetPosition.y;

        Assert.IsTrue(player.Motor.Jump());
        Assert.IsFalse(player.Motor.Jump(), "no double jump");
        float peak = groundY;
        for (int i = 0; i < 100; i++)
        {
            player.Motor.Tick(Vector2.zero, false, 0.01f);
            peak = Mathf.Max(peak, player.FeetPosition.y);
            if (i > 10 && player.Motor.IsGrounded) break;
        }

        Assert.AreEqual(config.jumpHeight, peak - groundY, 0.08f);
        Assert.IsTrue(player.Motor.IsGrounded, "lands again");

        Object.Destroy(player.gameObject);
        Object.Destroy(floor);
    }

    // A body in the air cannot twist around: steering mid-jump bends the path a little but
    // the facing stays (it used to swing sideways right after a shot).
    [UnityTest]
    public IEnumerator Airborne_SteeringDoesNotTurnTheBody()
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.localScale = new Vector3(10f, 0.2f, 10f);
        floor.transform.position = new Vector3(0f, -0.1f, 0f);
        var config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        PlayerEntity player = PlaceholderPlayerFactory.Create("Jumper", Basket.Core.TeamId.Home, config, Color.gray, Color.yellow);
        yield return null;
        PlayerMotor motor = player.Motor;
        for (int i = 0; i < 20 && !motor.IsGrounded; i++) motor.Tick(Vector2.zero, false, 0.02f);
        Quaternion start = player.transform.rotation;

        Assert.IsTrue(motor.Jump());
        float maxTurn = 0f;
        for (int i = 0; i < 150; i++)
        {
            motor.Tick(new Vector2(1f, 0f), false, 0.02f); // hard sideways input in the air
            if (motor.IsGrounded && i > 5) break;
            maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(start, player.transform.rotation));
        }
        Assert.Less(maxTurn, 1f, "no twisting in the air");
        Debug.Log($"Airborne steering: max turn {maxTurn:0.0} deg, landed {motor.IsGrounded} at {player.transform.position}");

        Object.Destroy(player.gameObject);
        Object.Destroy(floor);
    }
}
