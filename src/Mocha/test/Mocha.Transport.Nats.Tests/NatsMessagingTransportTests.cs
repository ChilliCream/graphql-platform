namespace Mocha.Transport.Nats.Tests;

public class NatsMessagingTransportTests
{
    [Fact]
    public void Constructor_Should_NotRunTheConfigureDelegate_When_TheTransportIsCreated()
    {
        // arrange
        var applied = false;

        // act
        _ = new NatsMessagingTransport(_ => applied = true);

        // assert
        Assert.False(applied);
    }
}
