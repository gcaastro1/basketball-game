using NUnit.Framework;
using UnityEngine;
using Basket.Core;

public class AIPerceptionTests
{
    [Test]
    public void Constructor_StoresAllFieldsExactly()
    {
        var self = new Vector3(1f, 0f, 2f);
        var opponent = new Vector3(3f, 0f, 4f);
        var ball = new Vector3(5f, 0f, 6f);

        var perception = new AIPerception(self, opponent, ball, opponentHasBall: true, selfHasBall: false);

        Assert.AreEqual(self, perception.SelfPosition);
        Assert.AreEqual(opponent, perception.OpponentPosition);
        Assert.AreEqual(ball, perception.BallPosition);
        Assert.IsTrue(perception.OpponentHasBall);
        Assert.IsFalse(perception.SelfHasBall);
    }
}
