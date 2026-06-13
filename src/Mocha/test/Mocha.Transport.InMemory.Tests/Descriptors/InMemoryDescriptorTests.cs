using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Descriptors;

public class InMemoryDescriptorTests
{
    [Fact]
    public void Transport_Should_DefaultAutoBindTrue_When_NotConfigured()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t => { });
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.True(transport.AutoBind);
    }

    [Fact]
    public void Transport_Should_SetAutoBindFalse_When_AutoBindFalseCalled()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t => t.AutoBind(false));
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        Assert.False(transport.AutoBind);
    }
}
