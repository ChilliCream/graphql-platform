namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroDownloadRetryPolicyTests
{
    [Theory]
    [InlineData(0, 1, 0, "attemptsWithCachedSeed")]
    [InlineData(1, 0, 0, "attemptsWithoutCachedSeed")]
    [InlineData(1, 1, -1, "delay")]
    public void Constructor_Should_Throw_When_TheBudgetCannotBeUsed(
        int attemptsWithCachedSeed,
        int attemptsWithoutCachedSeed,
        int delayInMilliseconds,
        string parameterName)
    {
        // act
        var exception = Record.Exception(() => new NitroDownloadRetryPolicy(
            attemptsWithCachedSeed,
            attemptsWithoutCachedSeed,
            TimeSpan.FromMilliseconds(delayInMilliseconds)));

        // assert
        Assert.Equal(
            parameterName,
            Assert.IsType<ArgumentOutOfRangeException>(exception).ParamName);
    }

    [Fact]
    public void GetAttempts_Should_SpendLess_When_ACachedSeedExists()
    {
        // arrange
        var policy = new NitroDownloadRetryPolicy(
            attemptsWithCachedSeed: 2,
            attemptsWithoutCachedSeed: 15,
            TimeSpan.FromSeconds(2));

        // act
        var attempts = (
            WithCachedSeed: policy.GetAttempts(hasCachedSeed: true),
            WithoutCachedSeed: policy.GetAttempts(hasCachedSeed: false));

        // assert
        Assert.Equal((2, 15), attempts);
    }
}
