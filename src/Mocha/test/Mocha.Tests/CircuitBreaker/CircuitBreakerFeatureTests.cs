namespace Mocha.Tests;

public class CircuitBreakerFeatureTests
{
    [Fact]
    public void CircuitBreakerFeature_Should_NotBeReadOnly_When_CreatedNew()
    {
        // arrange & act
        var feature = new CircuitBreakerFeature();

        // assert
        Assert.False(feature.IsReadOnly);
    }

    [Fact]
    public void CircuitBreakerFeature_Should_SetProperties_When_ConfigureIsCalled()
    {
        // arrange
        var feature = new CircuitBreakerFeature();

        // act
        feature.Configure(o =>
        {
            o.Enabled = true;
            o.FailureRatio = 0.3;
            o.MinimumThroughput = 2;
            o.SamplingDuration = TimeSpan.FromSeconds(5);
            o.BreakDuration = TimeSpan.FromSeconds(15);
        });

        // assert
        Assert.True(feature.Enabled);
        Assert.Equal(0.3, feature.FailureRatio);
        Assert.Equal(2, feature.MinimumThroughput);
        Assert.Equal(TimeSpan.FromSeconds(5), feature.SamplingDuration);
        Assert.Equal(TimeSpan.FromSeconds(15), feature.BreakDuration);
    }

    [Fact]
    public void CircuitBreakerFeature_Should_BecomeReadOnly_When_SealIsCalled()
    {
        // arrange
        var feature = new CircuitBreakerFeature();

        // act
        feature.Seal();

        // assert
        Assert.True(feature.IsReadOnly);
    }

    [Fact]
    public void CircuitBreakerFeature_Should_ThrowInvalidOperationException_When_ConfigureIsCalledAfterSeal()
    {
        // arrange
        var feature = new CircuitBreakerFeature();
        feature.Seal();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => feature.Configure(o => o.Enabled = true));
    }

    [Fact]
    public void CircuitBreakerFeature_Should_UseLastConfiguredValue_When_ConfigureIsCalledMultipleTimes()
    {
        // arrange
        var feature = new CircuitBreakerFeature();

        // act
        feature.Configure(o => o.FailureRatio = 0.1);
        feature.Configure(o => o.FailureRatio = 0.9);

        // assert
        Assert.Equal(0.9, feature.FailureRatio);
    }

    [Fact]
    public void CircuitBreakerFeature_Should_LeaveOtherPropertiesNull_When_ConfigureWithPartialSettings()
    {
        // arrange
        var feature = new CircuitBreakerFeature();

        // act - only set BreakDuration
        feature.Configure(o => o.BreakDuration = TimeSpan.FromSeconds(5));

        // assert - only BreakDuration is set
        Assert.Equal(TimeSpan.FromSeconds(5), feature.BreakDuration);
        Assert.Null(feature.Enabled);
        Assert.Null(feature.FailureRatio);
        Assert.Null(feature.MinimumThroughput);
        Assert.Null(feature.SamplingDuration);
    }

    [Fact]
    public void CircuitBreakerFeature_Should_AccumulateSettings_When_ConfigureIsCalledMultipleTimes()
    {
        // arrange
        var feature = new CircuitBreakerFeature();

        // act - configure in two separate calls
        feature.Configure(o => o.FailureRatio = 0.7);
        feature.Configure(o => o.MinimumThroughput = 3);

        // assert - both settings are present
        Assert.Equal(0.7, feature.FailureRatio);
        Assert.Equal(3, feature.MinimumThroughput);
    }
}
