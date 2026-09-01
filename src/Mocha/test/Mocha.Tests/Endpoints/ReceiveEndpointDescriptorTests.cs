using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class ReceiveEndpointDescriptorTests
{
    private static MessagingRuntime CreateRuntime(Action<IInMemoryMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddInMemory(configure);

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    [Fact]
    public void Temporary_Should_SetIsTemporaryOnConfiguration_When_Called()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.Endpoint("temporary-endpoint").Temporary());

        // assert
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "temporary-endpoint");
        Assert.True(endpoint.Configuration.IsTemporary);
    }

    [Fact]
    public void Temporary_Should_LeaveIsTemporaryFalse_When_NotCalled()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.Endpoint("default-endpoint"));

        // assert
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "default-endpoint");
        Assert.False(endpoint.Configuration.IsTemporary);
    }

    [Fact]
    public void Temporary_Should_ReturnDescriptor_When_Called_ForChaining()
    {
        // arrange & act
        var runtime = CreateRuntime(t => t.Endpoint("chained-endpoint").Temporary().MaxConcurrency(3));

        // assert
        var endpoint = (InMemoryReceiveEndpoint)runtime.Transports[0].ReceiveEndpoints
            .Single(e => e.Name == "chained-endpoint");
        Assert.True(endpoint.Configuration.IsTemporary);
        Assert.Equal(3, endpoint.Configuration.MaxConcurrency);
    }
}
