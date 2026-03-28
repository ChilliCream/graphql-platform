using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Topology;

public class NatsTopologyDescriptorTests
{
    [Fact]
    public void DeclareStream_Should_AddStreamToTopology_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
            b.AddNats(t => t.DeclareStream("orders").Subject("orders.>")));

        // assert
        Assert.Contains(topology.Streams, s => s.Name == "orders");
        var stream = topology.Streams.First(s => s.Name == "orders");
        Assert.Contains("orders.>", stream.Subjects);
    }

    [Fact]
    public void DeclareStream_Should_ApplyMaxAge_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
            b.AddNats(t => t.DeclareStream("orders").Subject("orders.>").MaxAge(TimeSpan.FromHours(24))));

        // assert
        var stream = topology.Streams.First(s => s.Name == "orders");
        Assert.Equal(TimeSpan.FromHours(24), stream.MaxAge);
    }

    [Fact]
    public void Endpoint_Should_CreateReceiveEndpoint_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(b =>
            b.AddNats(t => t.Endpoint("orders-ep").Handler<OrderCreatedHandler>()));

        // assert
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == "orders-ep");
    }

    [Fact]
    public void DispatchEndpoint_Should_CreateDispatchEndpoint_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(b =>
            b.AddNats(t => t.DispatchEndpoint("orders-dispatch").ToSubject("orders.created").Publish<OrderCreated>()));

        // assert
        Assert.Contains(transport.DispatchEndpoints, e => e.Name == "orders-dispatch");
    }

    [Fact]
    public void AutoProvision_Should_SetTopologyAutoProvision_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
            b.AddNats(t => t.AutoProvision(false)));

        // assert
        Assert.False(topology.AutoProvision);
    }

    [Fact]
    public void Url_Should_SetConnectionUrl_When_ConfiguredViaDescriptor()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
            b.AddNats(t => t.Url("nats://custom-host:4333")));

        // assert
        Assert.Contains("custom-host", topology.Address.ToString());
        Assert.Contains("4333", topology.Address.ToString());
    }

    private static (
        MessagingRuntime Runtime,
        NatsMessagingTransport Transport,
        NatsMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new NatsConnection(new NatsOpts { Url = "nats://localhost:4222" }));
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.BuildRuntime();
        var transport = runtime.Transports.OfType<NatsMessagingTransport>().Single();
        var topology = (NatsMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}
