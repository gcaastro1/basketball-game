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
}
