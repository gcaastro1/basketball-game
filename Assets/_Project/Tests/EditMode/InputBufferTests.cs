using NUnit.Framework;
using Basket.Input;

public class InputBufferTests
{
    [Test]
    public void PressIsRememberedForTheWindowOnly()
    {
        var b = new InputBuffer(0.15f);
        Assert.IsFalse(b.IsBuffered(1f));
        b.Press(1f);
        Assert.IsTrue(b.IsBuffered(1.1f));
        Assert.IsFalse(b.IsBuffered(1.2f));
    }

    [Test]
    public void Clear_ForgetsThePress()
    {
        var b = new InputBuffer(0.15f);
        b.Press(1f);
        b.Clear();
        Assert.IsFalse(b.IsBuffered(1f));
    }
}
