namespace Mocha.Tests;

public class ReceiveTypeBindDescriptorTests
{
    [Fact]
    public void ResolvedBindMode_Should_BeNull_When_NeitherBindModeCalled()
    {
        // arrange & act
        var descriptor = new ReceiveTypeBindDescriptor();

        // assert
        Assert.Null(descriptor.ResolvedBindMode);
    }

    [Fact]
    public void ResolvedBindMode_Should_BeImplicit_When_BindImplicitly()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.BindImplicitly();

        // assert
        Assert.Equal(MessagingBindMode.Implicit, descriptor.ResolvedBindMode);
    }

    [Fact]
    public void ResolvedBindMode_Should_BeExplicit_When_BindExplicitly()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.BindExplicitly();

        // assert
        Assert.Equal(MessagingBindMode.Explicit, descriptor.ResolvedBindMode);
    }
}
