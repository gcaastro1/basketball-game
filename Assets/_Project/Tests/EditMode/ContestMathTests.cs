using NUnit.Framework;
using UnityEngine;
using Basket.Gameplay;

public class ContestMathTests
{
    private static readonly Vector3 Shooter = Vector3.zero;
    private static readonly Vector3 Rim = new Vector3(0f, 3.05f, 7f);

    private static float C(Vector3 defender, bool airborne = false) => ContestMath.Contest(Shooter, Rim, defender, airborne, 2f, 0.3f);

    [Test]
    public void OutsideRadius_IsZero() => Assert.AreEqual(0f, C(new Vector3(0f, 0f, 2.5f)));

    [Test]
    public void Closer_IsMoreContested() => Assert.Greater(C(new Vector3(0f, 0f, 0.8f)), C(new Vector3(0f, 0f, 1.6f)));

    [Test]
    public void InFront_IsMoreContestedThanBehind() => Assert.Greater(C(new Vector3(0f, 0f, 1f)), C(new Vector3(0f, 0f, -1f)));

    [Test]
    public void Airborne_IsMoreContested() => Assert.Greater(C(new Vector3(0f, 0f, 1f), airborne: true), C(new Vector3(0f, 0f, 1f)));

    [Test]
    public void NeverExceedsOne() => Assert.LessOrEqual(C(new Vector3(0f, 0f, 0.01f), airborne: true), 1f);
}
