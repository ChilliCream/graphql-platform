using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests;

/// <summary>
/// Verifies the Phase 1 endpoint-owns-queue invariant: the endpoint is the sole creator of its
/// backing queue, and a parallel <c>DeclareQueue</c> with the same name folds instead of throwing.
/// </summary>
public class RabbitMQEndpointQueueOwnershipTests
{
    [Fact]
    public void Describe_Should_StayByteIdentical_When_EndpointQueueUntouched()
    {
        // arrange
        // A consumer registered via implicit binding with no explicit DeclareQueue. The endpoint
        // creates the queue through OnDiscoverTopology; this snapshot is the regression anchor
        // proving the endpoint-owns-queue move is transparent to callers.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_NotThrow_When_DeclareQueueAndEndpointShareName()
    {
        // arrange
        // Both configurations target the same queue via the Queue() builder. The resulting
        // topology must contain exactly one queue and one set of error/skipped queues.
        var withDeclare = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });

        var withoutDeclare = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });

        var transportWithDeclare = withDeclare.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var transportWithoutDeclare = withoutDeclare.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var describeWithDeclare = transportWithDeclare.Describe();
        var describeWithoutDeclare = transportWithoutDeclare.Describe();

        // assert
        // Both configurations must produce a single "orders" queue. The DeclareQueue path
        // must not produce a duplicate or cause an exception.
        new Snapshot()
            .Add(RabbitMQDescribeSnapshot.Create(describeWithDeclare), "WithDeclareQueue", MarkdownLanguages.Json)
            .Add(RabbitMQDescribeSnapshot.Create(describeWithoutDeclare), "WithoutDeclareQueue", MarkdownLanguages.Json)
            .MatchMarkdown();
    }

    [Fact]
    public void EndpointQueue_Should_PreserveDeclaredQueue_When_CollidesWithDeclareQueue()
    {
        // arrange
        // DeclareQueue adds the queue with Declared origin and AutoProvision=true.
        // The endpoint then materializes the same queue and reuses the declared entity.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").AutoProvision(true).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert
        Assert.Equal(TopologyOrigin.Declared, queue.Origin);
        Assert.True(queue.AutoProvision);
    }

    [Fact]
    public void EndpointQueue_Should_PreserveDeclaredArguments_When_CollidesWithDeclareQueue()
    {
        // arrange
        // DeclareQueue adds a queue with a custom argument. The endpoint then materializes
        // the same queue and reuses the declared entity.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").WithArgument("x-dead-letter-exchange", "orders_dlx").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert
        Assert.True(queue.Arguments.ContainsKey("x-dead-letter-exchange"));
        Assert.Equal("orders_dlx", queue.Arguments["x-dead-letter-exchange"]);
    }

    [Fact]
    public void EndpointQueue_Should_MaterializeDurableAutoDeleteQueueWithExpiry_When_EndpointMarkedTemporary()
    {
        // arrange
        // RabbitMQ 4.3 denies non-durable, non-exclusive queues, so Temporary() keeps the queue
        // durable and scopes its lifetime through auto-delete plus a queue expiry.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Endpoint("temp-orders").Temporary().Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "temp-orders");

        // assert
        DescribeQueue(queue).MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {
                "x-expires": 1800000
              }
            }
            """);
    }

    [Fact]
    public void EndpointQueue_Should_UseConfiguredExpiry_When_TemporaryCalledWithExpiry()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Endpoint("temp-orders").Temporary(TimeSpan.FromMinutes(5)).Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "temp-orders");

        // assert
        DescribeQueue(queue).MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {
                "x-expires": 300000
              }
            }
            """);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Endpoint_Should_Throw_When_TemporaryExpiryIsNotPositive(int seconds)
    {
        // arrange
        var expiry = TimeSpan.FromSeconds(seconds);

        // act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.Endpoint("temp-orders").Temporary(expiry).Consumer<OrderSpyConsumer>()));

        // assert
        Assert.Equal("expiry", exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EndpointQueue_Should_MaterializeTemporaryQueue_When_SharedQueueDiscoveredInEitherOrder(
        bool temporaryFirst)
    {
        // arrange
        var runtime = CreateRuntime(
            _ => { },
            t =>
            {
                t.BindExplicitly();

                var first = t.Endpoint("first");
                first.Extend().Configuration.QueueName = "shared";
                if (temporaryFirst)
                {
                    first.Temporary();
                }
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        var secondConfiguration = new RabbitMQReceiveEndpointConfiguration
        {
            Name = "second",
            QueueName = "shared",
            IsTemporary = !temporaryFirst
        };

        // act
        var second = transport.AddEndpoint(runtime, secondConfiguration);
        second.DiscoverTopology(runtime);
        var queue = topology.Queues.Single(q => q.Name == "shared");

        // assert
        DescribeQueue(queue).MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {
                "x-expires": 1800000
              }
            }
            """);
    }

    [Fact]
    public void EndpointQueue_Should_ThrowOnBuild_When_TemporaryEndpointConflictsWithDeclaredNonAutoDeleteQueue()
    {
        // arrange
        // A queue explicitly declared without auto-delete (the default) conflicts with a receive
        // endpoint for the same queue name marked Temporary(): the broker-native lifecycle it
        // requests can never be honored.
        var exception = Assert.Throws<InvalidOperationException>(() => CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Temporary().Consumer<OrderSpyConsumer>();
            }));

        // assert
        Assert.Equal(
            "Queue 'orders' is explicitly declared without auto-delete, which conflicts with receive "
            + "endpoint 'orders' being marked Temporary(). Declare the queue with AutoDelete(), or "
            + "remove Temporary() from the endpoint.",
            exception.Message);
    }

    [Fact]
    public void EndpointQueue_Should_ReuseDeclaredQueue_When_DeclaredAutoDeleteAndEndpointMarkedTemporary()
    {
        // arrange
        // A durable queue declared with auto-delete satisfies Temporary(); the declared queue is
        // kept as declared and no expiry is added on its behalf.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.DeclareQueue("orders").AutoDelete();
                t.Endpoint("orders").Temporary().Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var queue = topology.Queues.Single(q => q.Name == "orders");

        // assert
        DescribeQueue(queue).MatchInlineSnapshot(
            """
            {
              "Durable": true,
              "Exclusive": false,
              "AutoDelete": true,
              "Arguments": {}
            }
            """);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    private static object DescribeQueue(RabbitMQQueue queue)
        => new { queue.Durable, queue.Exclusive, queue.AutoDelete, queue.Arguments };

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
