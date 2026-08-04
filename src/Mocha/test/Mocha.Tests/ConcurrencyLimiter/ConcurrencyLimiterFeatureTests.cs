namespace Mocha.Tests;

public class ConcurrencyLimiterFeatureTests
{
    [Fact]
    public void ConcurrencyLimiterFeature_Should_HaveDefaultValues()
    {
        // arrange & act
        var feature = new ConcurrencyLimiterFeature();

        // assert
        Assert.False(feature.IsReadOnly);
        Assert.Null(feature.Enabled);
        Assert.Null(feature.MaxConcurrency);
    }

    [Fact]
    public void ConcurrencyLimiterFeature_Should_Configure_When_NotSealed()
    {
        // arrange
        var feature = new ConcurrencyLimiterFeature();

        // act
        feature.Configure(options =>
        {
            options.Enabled = true;
            options.MaxConcurrency = 10;
        });

        // assert
        Assert.True(feature.Enabled);
        Assert.Equal(10, feature.MaxConcurrency);
    }

    [Fact]
    public void ConcurrencyLimiterFeature_Should_ThrowException_When_ConfiguringAfterSealed()
    {
        // arrange
        var feature = new ConcurrencyLimiterFeature();
        feature.Seal();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => feature.Configure(options => options.Enabled = true));
    }
}
