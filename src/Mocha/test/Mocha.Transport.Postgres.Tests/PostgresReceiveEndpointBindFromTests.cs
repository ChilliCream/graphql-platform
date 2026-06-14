using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

/// <summary>
/// Verifies that BindFrom intents declared at queue level are materialized into topology
/// entities with topic-to-queue subscriptions during OnDiscoverTopology.
/// </summary>
public class PostgresReceiveEndpointBindFromTests
{
    [Fact]
    public void OnDiscoverTopology_Should_EmitSubscription_When_QueueBindFromDeclared()
    {
        // arrange
        // A queue-level BindFrom names a source topic. OnDiscoverTopology must ensure the topic
        // exists in the topology and create a topic-to-queue subscription.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("topic:source-topic"));
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void OnDiscoverTopology_Should_NotDuplicateSubscription_When_SameBindFromDeclaredTwice()
    {
        // arrange
        // Declaring the same queue-level BindFrom twice must produce exactly one subscription.
        // The existence guard in DeclareSubscription prevents a duplicate.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .BindFrom(new Uri("topic:source-topic"))
                    .BindFrom(new Uri("topic:source-topic"));
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var subscriptions = topology.Subscriptions
            .Where(s => s.Source.Name == "source-topic" && s.Destination.Name == "orders")
            .ToList();

        // assert
        Assert.Single(subscriptions);
    }

    [Fact]
    public void BindFrom_Should_FailBuild_When_PostgresRoutingKeyNonNull()
    {
        // arrange
        // PostgreSQL does not support routing keys; a BindFrom with a non-null routing key must
        // fail at topology discovery time with a targeted build error.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CreateRuntime(
                b => b.AddConsumer<OrderSpyConsumer>(),
                t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders")
                        .Consumer<OrderSpyConsumer>()
                        .BindFrom(new Uri("topic:source-topic"), routingKey: "key.not.valid");
                }));

        // assert
        Assert.Contains("PostgreSQL", ex.Message);
        Assert.Contains("routing key", ex.Message);
        Assert.Contains("orders", ex.Message);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                configureTransport(t);
            })
            .BuildRuntime();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
