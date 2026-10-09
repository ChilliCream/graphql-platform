namespace HotChocolate.Fusion.Execution.Clients;

public class HttpSourceSchemaClientConfigurationTests
{
    private static readonly Uri s_baseAddress = new("http://localhost:5000/graphql");

    [Fact]
    public void Constructor_Should_UseDefaultSubscriptionReadTimeout_When_NotSpecified()
    {
        // act
        var configuration = new HttpSourceSchemaClientConfiguration("A", s_baseAddress);

        // assert
        Assert.Equal(TimeSpan.FromSeconds(60), configuration.SubscriptionReadTimeout);
    }

    [Fact]
    public void Constructor_Should_AcceptInfiniteTimeSpan_When_SubscriptionReadTimeoutIsDisabled()
    {
        // act
        var configuration = new HttpSourceSchemaClientConfiguration(
            "A",
            s_baseAddress,
            subscriptionReadTimeout: Timeout.InfiniteTimeSpan);

        // assert
        Assert.Equal(Timeout.InfiniteTimeSpan, configuration.SubscriptionReadTimeout);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-10_000_000L)]
    [InlineData(long.MaxValue)]
    public void Constructor_Should_ThrowArgumentOutOfRangeException_When_SubscriptionReadTimeoutIsOutOfRange(
        long ticks)
    {
        // act
        var exception = Record.Exception(
            () => new HttpSourceSchemaClientConfiguration(
                "A",
                s_baseAddress,
                subscriptionReadTimeout: TimeSpan.FromTicks(ticks)));

        // assert
        var argumentException = Assert.IsType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("subscriptionReadTimeout", argumentException.ParamName);
    }
}
