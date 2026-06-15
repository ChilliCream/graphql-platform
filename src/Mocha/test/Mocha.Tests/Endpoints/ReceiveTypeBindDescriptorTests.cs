namespace Mocha.Tests;

public class ReceiveTypeBindDescriptorTests
{
    [Fact]
    public void ResolvedAutoBind_Should_BeNull_When_NeitherAutoBindCalled()
    {
        // arrange & act
        var descriptor = new ReceiveTypeBindDescriptor();

        // assert
        Assert.Null(descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void ResolvedAutoBind_Should_BeTrue_When_AutoBindTrue()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.AutoBind(true);

        // assert
        Assert.Equal(true, descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void ResolvedAutoBind_Should_BeFalse_When_AutoBindFalse()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.AutoBind(false);

        // assert
        Assert.Equal(false, descriptor.ResolvedAutoBind);
    }
}
