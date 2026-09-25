using NUnit.Framework;
using Basket.Gameplay;

public class DribbleMathTests
{
    [Test]
    public void ComputeBounceOffsetY_AtPhaseZero_ReturnsMaxNegativeOffset()
    {
        float offset = DribbleMath.ComputeBounceOffsetY(phase: 0f, bounceHeight: 0.35f);
        Assert.AreEqual(-0.35f, offset, 0.001f);
    }

    [Test]
    public void ComputeBounceOffsetY_StaysWithinBounceHeightRange()
    {
        for (float phase = 0f; phase < 10f; phase += 0.3f)
        {
            float offset = DribbleMath.ComputeBounceOffsetY(phase, bounceHeight: 0.35f);
            Assert.LessOrEqual(offset, 0f + 0.001f);
            Assert.GreaterOrEqual(offset, -0.35f - 0.001f);
        }
    }
}
