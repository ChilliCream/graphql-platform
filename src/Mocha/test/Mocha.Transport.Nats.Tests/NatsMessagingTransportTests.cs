using Xunit;

namespace Mocha.Transport.Nats.Tests;

public class NatsMessagingTransportTests
{
    [Fact]
    public void Constructor_Should_NotRunTheConfigureDelegate_When_TheTransportIsCreated()
    {
        // arrange
        // The delegate runs when the bus initializes the transport, not when it is constructed,
        // because it needs the setup context to build a descriptor against.
        var applied = false;

        // act
        _ = new NatsMessagingTransport(_ => applied = true);

        // assert
        Assert.False(applied);
    }
}
