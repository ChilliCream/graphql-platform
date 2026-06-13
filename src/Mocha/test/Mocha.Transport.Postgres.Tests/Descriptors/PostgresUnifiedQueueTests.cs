using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

/// <summary>
/// Verifies the identity, entity-only lowering, rename guards, and convergence behavior of the
/// unified <c>t.Queue(name, q => ...)</c> front door on the PostgreSQL transport.
/// </summary>
public class PostgresUnifiedQueueTests
{
    // --- Identity ---

    [Fact]
    public void Queue_Should_ResolveSameEndpoint_When_EndpointSharesQueueName()
    {
        // arrange
        // Two paths target the same queue name "orders":
        //   1. t.Endpoint("ep").Queue("orders") explicit endpoint with a queue rename
        //   2. t.Queue("orders") unified front door
        // Both must converge on a single receive endpoint, not create a duplicate.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("ep").Queue("orders").Consumer<OrderSpyConsumer>();
                t.Queue("orders").AutoBind(false);
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
                t.BindHandlersExplicitly();
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
                t.BindHandlersExplicitly();
                var first = t.Queue("orders");
                first.Consumer<OrderSpyConsumer>();
                var second = t.Queue("orders");
                second.AutoBind(false);
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
    public void Queue_Should_LowerToEntityOnly_When_NoConsumersOrReceives()
    {
        // arrange
        // An entity-only Queue() handle (no consumer, no Receives) lowers to a declared queue
        // entity and its BindFrom subscriptions but never enters the receive-endpoint lifecycle.
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("audit", q => q.BindFrom(new Uri("topic:audit-events")));
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .FirstOrDefault(e => e.Queue.Name == "audit");

        // assert: no receive endpoint was created for the entity-only queue
        Assert.Null(endpoint);

        // assert: the queue entity was lowered into the topology
        Assert.Contains(topology.Queues, q => q.Name == "audit");

        // assert: the BindFrom subscription and its source topic were lowered
        Assert.Contains(topology.Topics, t => t.Name == "audit-events");
        Assert.Contains(topology.Subscriptions, s => s.Source.Name == "audit-events" && s.Destination.Name == "audit");
    }

    [Fact]
    public void Queue_Should_NotMaterializeReceiveEndpoint_When_NoConsumersOrReceives()
    {
        // arrange
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindHandlersExplicitly();
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

    // --- Rename guards ---

    [Fact]
    public void Queue_Should_Throw_When_QueueNameChangedAfterPinned()
    {
        // arrange
        // Once an identity-pinned Queue() handle exists, calling Queue("other-name") on the
        // returned descriptor must fail at build time, not silently rename the queue.
        void Build()
        {
            CreateRuntime(
                b => { },
                t =>
                {
                    t.BindHandlersExplicitly();
                    IPostgresReceiveEndpointDescriptor handle = t.Queue("orders");

                    // downcast to the base interface and attempt a rename
                    handle.Queue("different-name");
                });
        }

        // act
        var exception = Assert.ThrowsAny<InvalidOperationException>(Build);

        // assert
        Assert.Contains("orders", exception.Message);
    }

    [Fact]
    public void Queue_Should_Throw_When_ObsoleteQueueMethodCalledOnFrontDoor()
    {
        // arrange
        // The IPostgresQueueEndpointDescriptor.Queue(string) method is decorated with
        // [Obsolete(error: true)]. A runtime call through the concrete adapter must also throw.
        void Build()
        {
            CreateRuntime(
                b => { },
                t =>
                {
                    t.BindHandlersExplicitly();
                    var handle = t.Queue("orders");

                    // call the guarded method via the adapter's explicit guard
                    ((IPostgresReceiveEndpointDescriptor)handle).Queue("should-throw");
                });
        }

        // act
        var exception = Assert.ThrowsAny<InvalidOperationException>(Build);

        // assert: the identity-pinned queue name is mentioned in the error
        Assert.Contains("orders", exception.Message);
    }

    // --- Build errors ---

    [Fact]
    public void Queue_Should_Throw_When_TwoEndpointsShareSameQueueName()
    {
        // arrange
        void Build()
        {
            CreateRuntime(
                b => b.AddConsumer<OrderSpyConsumer>(),
                t =>
                {
                    t.BindHandlersExplicitly();
                    t.Queue("orders").Consumer<OrderSpyConsumer>();
                    // A second endpoint targeting the same queue name "orders" must be rejected
                    t.Endpoint("second").Queue("orders").Consumer<OrderSpyConsumer>();
                });
        }

        // act & assert: two endpoints sharing one queue name is a build error
        Assert.ThrowsAny<InvalidOperationException>(Build);
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
                    t.BindHandlersExplicitly();
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
                t.BindHandlersExplicitly();
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
