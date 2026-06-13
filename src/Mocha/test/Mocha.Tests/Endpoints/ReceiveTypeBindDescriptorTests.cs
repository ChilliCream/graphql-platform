namespace Mocha.Tests;

public class ReceiveTypeBindDescriptorTests
{
    [Fact]
    public void BindFrom_Should_ImplyAutoBindFalse_When_NoExplicitAutoBind()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.BindFrom(new Uri("exchange:my-exchange"));

        // assert
        Assert.Equal(false, descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void AutoBind_Should_WinOverImplicit_When_ExplicitlyTrueWithBindFrom()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act: call order must not matter; explicit wins regardless
        descriptor.BindFrom(new Uri("exchange:my-exchange"));
        descriptor.AutoBind(true);

        // assert
        Assert.Equal(true, descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void AutoBind_Should_WinOverImplicit_When_ExplicitlyTrueCalledBeforeBindFrom()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();

        // act
        descriptor.AutoBind(true);
        descriptor.BindFrom(new Uri("exchange:my-exchange"));

        // assert
        Assert.Equal(true, descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void ResolvedAutoBind_Should_BeNull_When_NeitherAutoBindNorBindFromCalled()
    {
        // arrange & act
        var descriptor = new ReceiveTypeBindDescriptor();

        // assert
        Assert.Null(descriptor.ResolvedAutoBind);
    }

    [Fact]
    public void BindFrom_Should_AccumulateIntents_When_CalledMultipleTimes()
    {
        // arrange
        var descriptor = new ReceiveTypeBindDescriptor();
        var source1 = new Uri("exchange:ex-1");
        var source2 = new Uri("exchange:ex-2");

        // act
        descriptor.BindFrom(source1, "key-a");
        descriptor.BindFrom(source2);

        // assert
        Assert.Equal(2, descriptor.BindFroms.Count);
        Assert.Equal(source1, descriptor.BindFroms[0].Source);
        Assert.Equal("key-a", descriptor.BindFroms[0].RoutingKey);
        Assert.Equal(source2, descriptor.BindFroms[1].Source);
        Assert.Null(descriptor.BindFroms[1].RoutingKey);
    }
}
