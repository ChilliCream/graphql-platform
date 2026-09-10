using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies that BindFrom intents declared at queue level and per-type level are materialized
/// into topology entities with declared origin during OnDiscoverTopology.
/// </summary>
public class RabbitMQReceiveEndpointBindFromTests
{
    [Fact]
    public void OnDiscoverTopology_Should_AddDeclaredBinding_When_QueueBindFromDeclared()
    {
        // arrange
        // A queue-level BindFrom names a source exchange. OnDiscoverTopology must add the exchange
        // to the topology and create an exchange-to-queue binding with declared origin.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders")
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
                t.BindExplicitly();
                t.Queue("orders")
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
    public void OnDiscoverTopology_Should_InheritQueueAutoProvision_When_QueueOptsIn()
    {
        // arrange
        // With the transport deny-by-default, a queue that opts in to provisioning must carry that
        // opt-in onto the binding its BindFrom derives; the foreign source exchange stays opted out.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(false);
                t.BindExplicitly();
                t.Queue("orders")
                    .AutoProvision(true)
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"), "order.created");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Source.Name == "source-fanout-exchange" && b.Destination.Name == "orders");
        var exchange = topology.Exchanges.Single(e => e.Name == "source-fanout-exchange");

        // assert
        Assert.True(binding.AutoProvision);
        Assert.Null(exchange.AutoProvision);
    }

    [Fact]
    public void OnDiscoverTopology_Should_LeaveBindingAutoProvisionUnset_When_QueueDoesNotOptIn()
    {
        // arrange
        // A queue without its own AutoProvision leaves the derived binding on the transport default.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"), "order.created");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Source.Name == "source-fanout-exchange" && b.Destination.Name == "orders");

        // assert
        Assert.Null(binding.AutoProvision);
    }

    [Fact]
    public void OnDiscoverTopology_Should_KeepBindingOptOut_When_QueueOptsIn()
    {
        // arrange
        // A binding can opt out of provisioning individually even though its queue opts in.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(false);
                t.BindExplicitly();
                t.Queue("orders")
                    .AutoProvision(true)
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"), "order.created", false);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Source.Name == "source-fanout-exchange" && b.Destination.Name == "orders");

        // assert
        Assert.False(binding.AutoProvision);
    }

    [Fact]
    public void OnDiscoverTopology_Should_ProvisionBinding_When_OnlyBindingOptsIn()
    {
        // arrange
        // A binding can opt in individually even though its queue does not.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoProvision(false);
                t.BindExplicitly();
                t.Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"), "order.created", autoProvision: true);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Source.Name == "source-fanout-exchange" && b.Destination.Name == "orders");

        // assert
        Assert.True(binding.AutoProvision);
    }

    [Fact]
    public void OnDiscoverTopology_Should_InheritQueueOptOut_When_TransportProvisionsByDefault()
    {
        // arrange
        // Inheritance also carries an opt-out: a queue that is managed externally drags its
        // BindFrom binding out of provisioning even though the transport provisions by default.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders")
                    .AutoProvision(false)
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("exchange:source-fanout-exchange"), "order.created");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var binding = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Single(b => b.Source.Name == "source-fanout-exchange" && b.Destination.Name == "orders");

        // assert
        Assert.False(binding.AutoProvision);
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
