using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Descriptors;

public class InMemoryDescriptorTests
{
    [Fact]
    public void Transport_Should_DefaultBindModeImplicit_When_NotConfigured()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t => { });
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Implicit, transport.BindMode);
    }

    [Fact]
    public void Transport_Should_SetBindModeExplicit_When_BindExplicitlyCalled()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.Equal(MessagingBindMode.Explicit, transport.BindMode);
    }
}
