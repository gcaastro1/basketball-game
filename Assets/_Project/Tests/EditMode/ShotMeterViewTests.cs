using NUnit.Framework;
using Basket.Core;
using Basket.Presentation;

// The meter bar: up to the top at the jump apex (the green is at the top), then back down.
public class ShotMeterViewTests
{
    [Test]
    public void Bar_RisesToTheTopAtTheApex_ThenFallsBack()
    {
        const float apex = 0.4f;
        Assert.AreEqual(0f, ShotMeterView.BarPosition(0f, apex), 1e-5f);
        Assert.AreEqual(0.5f, ShotMeterView.BarPosition(0.2f, apex), 1e-5f);
        Assert.AreEqual(1f, ShotMeterView.BarPosition(apex, apex), 1e-5f, "top = the apex = the green");
        Assert.AreEqual(0.5f, ShotMeterView.BarPosition(0.6f, apex), 1e-5f, "late: back down");
        Assert.AreEqual(0f, ShotMeterView.BarPosition(1f, apex), 1e-5f);
        float greenBottom = ShotMeterView.GreenBottom(apex, 0.02f);
        Assert.AreEqual(0.95f, greenBottom, 1e-5f, "the green is the top of the bar");
    }

    [Test]
    public void Labels_InPortuguese()
    {
        Assert.AreEqual("PERFEITO!", ShotMeterView.Label(ShotTimingGrade.Perfect));
        Assert.AreEqual("MUITO CEDO", ShotMeterView.Label(ShotTimingGrade.VeryEarly));
        Assert.AreEqual("POUCO TARDE", ShotMeterView.Label(ShotTimingGrade.SlightlyLate));
    }
}
