namespace Mocha.Tests.Transport;

public sealed class TransportShutdownOptionsTests
{
    [Fact]
    public void CancellationGracePeriod_Should_Throw_When_ValueIsInfinite()
    {
        // arrange
        var options = new TransportShutdownOptions();

        // act
        Action act = () => options.CancellationGracePeriod = Timeout.InfiniteTimeSpan;

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void CleanupTimeout_Should_Throw_When_ValueIsNegative()
    {
        // arrange
        var options = new TransportShutdownOptions();

        // act
        Action act = () => options.CleanupTimeout = TimeSpan.FromSeconds(-5);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Shutdown_Should_Throw_When_SetToNull()
    {
        // arrange
        var options = new TransportOptions();

        // act
        Action act = () => options.Shutdown = null!;

        // assert
        Assert.Throws<ArgumentNullException>(act);
    }
}
