using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies that BindFrom intents declared at queue level and per-type level are materialized
/// into topology entities with declared provenance during OnDiscoverTopology.
/// </summary>
public class RabbitMQReceiveEndpointBindFromTests
{
    [Fact]
    public void OnDiscoverTopology_Should_AddDeclaredBinding_When_QueueBindFromDeclared()
    {
        // arrange
        // A queue-level BindFrom names a source exchange. OnDiscoverTopology must add the exchange
        // to the topology and create an exchange-to-queue binding with declared provenance.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders")
                    .Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void OnDiscoverTopology_Should_AddTwoBindings_When_TwoRoutingKeysDeclared()
    {
        // arrange
        // Two queue-level BindFrom intents with the same source exchange but different routing keys
        // must each produce a distinct binding; no deduplication across different keys.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders")
                    .Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:topic-exchange"), "order.created.eu")
                    .BindFrom(new Uri("exchange:topic-exchange"), "order.created.us");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var description = transport.Describe();
        var bindings = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Where(b => b.Source.Name == "topic-exchange" && b.Destination.Name == "orders")
            .ToList();

        // assert
        Assert.Equal(2, bindings.Count);
        Assert.Equal(2, bindings.Select(b => b.RoutingKey).Distinct().Count());
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void OnDiscoverTopology_Should_AddPerTypeBindings_When_TypeBindFromDeclared()
    {
        // arrange
        // A per-type BindFrom via Receives<T>(r => r.BindFrom(...)) must be materialized as a
        // declared exchange-to-queue binding. The convention's auto-bind for that type is suppressed
        // (AutoBind false implied), so only the explicit binding appears.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders")
                    .Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .Receives<OrderCreated>(r => r.BindFrom(new Uri("exchange:custom-orders-exchange")));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        // The explicit binding from custom-orders-exchange into orders must appear; the convention
        // publish/send chain for OrderCreated must NOT be present because AutoBind is implied false
        // by the per-type BindFrom.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
