using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Basket.Gameplay;

public class CameraControllerTests
{
    [UnityTest]
    public IEnumerator Configure_WithTarget_CameraMovesTowardOffsetPosition()
    {
        var camGo = new GameObject("Cam");
        var controller = camGo.AddComponent<CameraController>();
        controller.SetConfigForTest(ScriptableObject.CreateInstance<CameraConfig>());

        var targetGo = new GameObject("Target");
        targetGo.transform.position = new Vector3(10f, 0f, 10f);

        controller.Configure(targetGo.transform);

        Vector3 startPos = camGo.transform.position;
        for (int i = 0; i < 10; i++) yield return null;

        Assert.Less(Vector3.Distance(camGo.transform.position, targetGo.transform.position),
                    Vector3.Distance(startPos, targetGo.transform.position));

        Object.Destroy(camGo);
        Object.Destroy(targetGo);
    }

    // Broadcast mode: behind the player facing the attacked basket; when the attack changes
    // ends (full court), the camera swings around to face the other basket.
    [UnityTest]
    public IEnumerator Broadcast_FacesTheAttackedBasket_AndTurnsWhenTheAttackChangesEnds()
    {
        var camGo = new GameObject("Cam");
        camGo.AddComponent<Camera>();
        var controller = camGo.AddComponent<CameraController>();
        var config = ScriptableObject.CreateInstance<CameraConfig>();
        var targetGo = new GameObject("Target");
        targetGo.transform.position = new Vector3(2f, 0f, 14f);
        Vector3 north = new Vector3(0f, 3.05f, 26.8f), south = new Vector3(0f, 3.05f, 1.2f);
        Vector3 attacked = north;
        controller.Configure(targetGo.transform, config, () => attacked, new Vector3(0f, 0f, 14f));

        for (int i = 0; i < 5; i++) yield return null;
        Assert.Less(camGo.transform.position.z, targetGo.transform.position.z, "behind the player");
        Assert.Greater(camGo.transform.forward.z, 0.5f, "looking at the north basket");

        attacked = south;
        yield return null;
        Assert.Greater(controller.Direction.z, 0f, "turns smoothly, not in one frame");
        float elapsed = 0f;
        while (elapsed < 3f)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        Assert.Greater(camGo.transform.position.z, targetGo.transform.position.z, "now behind on the other side");
        Assert.Less(camGo.transform.forward.z, -0.5f, "looking at the south basket");

        Object.Destroy(camGo);
        Object.Destroy(targetGo);
    }
}
