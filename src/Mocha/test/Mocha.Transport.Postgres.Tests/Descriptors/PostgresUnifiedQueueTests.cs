using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

/// <summary>
/// Verifies the identity, entity-only lowering, and convergence behavior of the
/// unified <c>t.Queue(name, q => ...)</c> front door on the PostgreSQL transport.
/// </summary>
public class PostgresUnifiedQueueTests
{
    // --- Identity ---

    [Fact]
    public void Queue_Should_ResolveSameEndpoint_When_EndpointSharesQueueName()
    {
        // arrange
        // Queue("orders") with a consumer must produce exactly one receive endpoint,
        // not create a duplicate.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>().BindExplicitly();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoints = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Where(e => e.Queue.Name == "orders")
            .ToList();

        // assert: exactly one endpoint for "orders", not two
        Assert.Single(endpoints);
    }

    [Fact]
    public void Queue_Should_CreateEndpoint_When_ConsumerAttached()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "orders");

        // assert: the unified handle materialized exactly one receive endpoint named "orders"
        Assert.NotNull(endpoint);
        Assert.Equal("orders", endpoint.Queue.Name);
    }

    [Fact]
    public void Queue_Should_ResolveIdenticalHandle_When_CalledTwiceWithSameName()
    {
        // arrange
        // Calling t.Queue("orders") a second time must return the same backing endpoint adapter,
        // not create a second receive endpoint.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                var first = t.Queue("orders");
                first.Consumer<OrderSpyConsumer>();
                var second = t.Queue("orders");
                second.BindExplicitly();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoints = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Where(e => e.Queue.Name == "orders")
            .ToList();

        // assert: only one endpoint exists for "orders"
        Assert.Single(endpoints);
    }

    // --- Entity-only lowering ---

    [Fact]
    public void Queue_Should_NotMaterializeReceiveEndpoint_When_NoConsumersOrReceives()
    {
        // arrange
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("dispatch-target");
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .FirstOrDefault(e => e.Queue.Name == "dispatch-target");

        // assert: no receive endpoint, but the queue entity exists
        Assert.Null(endpoint);
        Assert.Contains(topology.Queues, q => q.Name == "dispatch-target");
    }

    // --- Identity guards ---

    [Fact]
    public void Queue_Should_SetQueueNameAsEndpointName_When_NoExplicitEndpointName()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("my-queue").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "my-queue");

        // assert: the builder used the queue name as the endpoint name
        Assert.NotNull(endpoint);
        Assert.Equal("my-queue", endpoint.Name);
    }

    // --- Build errors ---

    [Fact]
    public void Queue_Should_MergeWithExistingEndpoint_When_SameNameCalledTwice()
    {
        // arrange
        // Calling Queue("orders") when Endpoint("orders") already created an endpoint with
        // that name must merge onto the existing endpoint, not create a second one.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Endpoint("orders");
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var endpoints = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Where(e => e.Queue.Name == "orders")
            .ToList();

        // assert: exactly one endpoint, not two
        Assert.Single(endpoints);
    }

    [Fact]
    public void Queue_Should_Throw_When_SatelliteConfiguredOnEntityOnlyQueue()
    {
        // arrange
        void Build()
        {
            CreateRuntime(
                b => { },
                t =>
                {
                    t.BindExplicitly();
                    // No consumer or Receives: entity-only. Configuring an error satellite
                    // on an entity-only queue must fail because there is no consumer to process
                    // the failed messages.
                    t.Queue("audit").ErrorQueue("audit-error");
                });
        }

        // act & assert: satellite on entity-only queue is a build error
        Assert.ThrowsAny<InvalidOperationException>(Build);
    }

    // --- AutoProvision ---

    [Fact]
    public void Queue_Should_PropagateAutoProvision_When_AutoProvisionCalled()
    {
        // arrange
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit").AutoProvision(false);
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.SingleOrDefault(q => q.Name == "audit");

        // assert: AutoProvision(false) propagated to the lowered queue entity
        Assert.NotNull(queue);
        Assert.Equal(false, queue.AutoProvision);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
