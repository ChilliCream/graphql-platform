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

    [Fact]
    public void Endpoint_Should_SetIsTemporaryOnConfiguration_When_TemporaryCalled()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t => t.Endpoint("temporary-endpoint").Temporary());
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "temporary-endpoint");

        // assert
        Assert.True(endpoint.Configuration.IsTemporary);
    }

    [Fact]
    public void Queue_Should_SetIsTemporaryOnReceiveEndpoint_When_TemporaryCalled()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("temporary-queue").Temporary();
            });
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "temporary-queue");

        // assert
        Assert.True(endpoint.Configuration.IsTemporary);
    }

    [Fact]
    public void Queue_Should_LeaveIsTemporaryFalse_When_NotCalled()
    {
        // arrange & act
        var runtime = InMemoryBusFixture.CreateRuntimeWithTransport(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("default-queue");
            });
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "default-queue");

        // assert
        Assert.False(endpoint.Configuration.IsTemporary);
    }
}
