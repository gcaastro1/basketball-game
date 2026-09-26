using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;
using Basket.Input;

// Broadcast follow camera: behind the player, facing the basket being attacked.
public class CameraRigMathTests
{
    private static readonly Vector3 Hoop = new Vector3(0f, 3.05f, 13.1f);
    private static readonly Vector3 Center = new Vector3(0f, 0f, 7f);

    private static CameraConfig Config(float aim = 0f)
    {
        var c = ScriptableObject.CreateInstance<CameraConfig>();
        c.aimAtHoop = aim;
        return c;
    }

    [Test]
    public void Direction_Broadcast_LooksStraightDownTheCourt_WhereverThePlayerIs()
    {
        foreach (float x in new[] { -6f, 0f, 5f })
        {
            Vector3 d = CameraRigMath.Direction(new Vector3(x, 0f, 6f), Hoop, Center, 0f);
            Assert.AreEqual(0f, d.x, 1e-4f);
            Assert.AreEqual(1f, d.z, 1e-4f, "toward the attacked basket");
        }
    }

    [Test]
    public void Direction_TurnsTowardTheRim_WithAimAtHoop()
    {
        Vector3 wing = new Vector3(-6f, 0f, 9f);
        Vector3 full = CameraRigMath.Direction(wing, Hoop, Center, 1f);
        Vector3 toRim = new Vector3(Hoop.x - wing.x, 0f, Hoop.z - wing.z).normalized;
        Assert.Greater(Vector3.Dot(full, toRim), 0.999f);

        Vector3 half = CameraRigMath.Direction(wing, Hoop, Center, 0.5f);
        Assert.Greater(half.x, 0f, "partly turned toward the rim");
        Assert.Less(half.x, full.x);
    }

    [Test]
    public void Direction_FullCourt_FacesWhicheverBasketIsAttacked()
    {
        var center = new Vector3(0f, 0f, 14f);
        var north = new Vector3(0f, 3.05f, 26.8f);
        var south = new Vector3(0f, 3.05f, 1.2f);
        Vector3 player = new Vector3(2f, 0f, 14f);
        Assert.Greater(CameraRigMath.Direction(player, north, center, 0.3f).z, 0.9f);
        Assert.Less(CameraRigMath.Direction(player, south, center, 0.3f).z, -0.9f);
    }

    [Test]
    public void Place_BehindAndAbove_LookingAheadButNeverPastTheRim()
    {
        CameraConfig c = Config();
        Vector3 player = new Vector3(1f, 0f, 6f);
        var rig = CameraRigMath.Place(player, Hoop, Vector3.forward, c);

        Assert.AreEqual(player.z - c.distance, rig.Position.z, 1e-4f, "behind the player");
        Assert.AreEqual(c.height, rig.Position.y, 1e-4f);
        Assert.AreEqual(player.x, rig.Position.x, 1e-4f);
        Assert.Greater(rig.LookAt.z, player.z, "looks ahead of the player");
        Assert.LessOrEqual(rig.LookAt.z - player.z, c.maxLookAhead + 1e-4f);

        var under = CameraRigMath.Place(new Vector3(0f, 0f, 13.5f), Hoop, Vector3.forward, c);
        Assert.AreEqual(13.5f, under.LookAt.z, 1e-4f, "past the rim: looks at the player");
    }

    // Movement input follows the camera: "up" is where it looks, on either end of the court.
    [Test]
    public void Input_Up_MovesWhereTheCameraLooks()
    {
        Vector2 up = new Vector2(0f, 1f), right = new Vector2(1f, 0f);
        AssertClose(up, CameraRelativeMove.Rotate(up, new Vector3(0f, -0.4f, 1f)), "facing +z: unchanged");
        AssertClose(new Vector2(0f, -1f), CameraRelativeMove.Rotate(up, new Vector3(0f, -0.4f, -1f)), "other end: up goes -z");
        AssertClose(new Vector2(-1f, 0f), CameraRelativeMove.Rotate(right, new Vector3(0f, 0f, -1f)), "other end: right goes -x");
        AssertClose(new Vector2(1f, 0f), CameraRelativeMove.Rotate(up, new Vector3(1f, 0f, 0f)), "facing +x");
        AssertClose(new Vector2(0f, -1f), CameraRelativeMove.Rotate(right, new Vector3(1f, 0f, 0f)), "facing +x: right goes -z");
        AssertClose(up, CameraRelativeMove.Rotate(up, Vector3.down), "looking straight down: unchanged");
    }

    private static void AssertClose(Vector2 expected, Vector2 actual, string message)
    {
        Assert.AreEqual(expected.x, actual.x, 1e-4f, message);
        Assert.AreEqual(expected.y, actual.y, 1e-4f, message);
    }
}
