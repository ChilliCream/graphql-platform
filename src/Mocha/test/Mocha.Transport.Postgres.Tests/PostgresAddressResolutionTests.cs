using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

/// <summary>
/// Verifies that endpoint addresses written in the full transport form, rather than the short
/// <c>postgres:q/{name}</c> form, resolve against the transport topology address.
/// </summary>
public class PostgresAddressResolutionTests
{
    [Fact]
    public void Topology_Should_RegisterFaultQueue_When_FaultAddressIsTopologyAddress()
    {
        // arrange & act
        var topology = CreateTopology(t =>
            t.Queue("orders").AutoProvision(true).Handler<OrderCreatedHandler>()
                .FaultEndpoint(new Uri("postgres://localhost:5432/q/orders-errors")));

        // assert
        Assert.Contains(topology.Queues, q => q.Name == "orders-errors");
    }

    [Fact]
    public void Topology_Should_RegisterFaultQueue_When_FaultAddressOmitsHost()
    {
        // arrange & act
        // the short form the routing strategy produces for conventional fault endpoints
        var topology = CreateTopology(t =>
            t.Queue("orders").AutoProvision(true).Handler<OrderCreatedHandler>()
                .FaultEndpoint(new Uri("postgres:q/orders-errors")));

        // assert
        Assert.Contains(topology.Queues, q => q.Name == "orders-errors");
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ResolveQueue_When_AddressIsTopologyAddress()
    {
        // arrange
        var runtime = CreateRuntime(t => t.DeclareQueue("orders"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var queue = ((PostgresMessagingTopology)transport.Topology).Queues.Single(q => q.Name == "orders");

        // act
        var endpoint = runtime.GetDispatchEndpoint(queue.Address);

        // assert
        Assert.Equal("q/orders", endpoint.Name);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ResolveTopic_When_AddressIsTopologyAddress()
    {
        // arrange
        var runtime = CreateRuntime(t => t.DeclareTopic("events"));
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topic = ((PostgresMessagingTopology)transport.Topology).Topics.Single(t => t.Name == "events");

        // act
        var endpoint = runtime.GetDispatchEndpoint(topic.Address);

        // assert
        Assert.Equal("t/events", endpoint.Name);
    }

    private static PostgresMessagingTopology CreateTopology(
        Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            configureTransport(t);
        });

        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        return (PostgresMessagingTopology)transport.Topology;
    }

    private static MessagingRuntime CreateRuntime(Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();

        return builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                configureTransport(t);
            })
            .BuildRuntime();
    }
}
