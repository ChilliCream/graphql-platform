using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using CookieCrumble;

namespace Mocha.Transport.RabbitMQ.Tests.Routing;

/// <summary>
/// Verifies that <see cref="RabbitMQReceiveEndpointTopologyConvention"/> resolves its chain entry
/// through the transport destination resolver: it honors an explicit publish exchange, converges with
/// the producer path on the same entity, fails the build for an underivable consume bind (a per-message
/// routing key or an explicit queue destination), and never consults the resolver for reply routes.
/// </summary>
public class RabbitMQReceiveTopologyConventionTests
{
    [Fact]
    public void DiscoverTopology_Should_BindQueueFromExplicitExchange_When_PublishDestinationConfigured()
    {
        // arrange
        // OrderCreated is consumed and names an explicit publish exchange, so the convention must bind
        // that exchange directly into the endpoint queue instead of the convention publish exchange.
        var runtime = CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQExchange("custom-orders")));
            },
            t => t.BindHandlersImplicitly());
        var (topology, queueName) = ResolveConsumerQueue(runtime);

        // act
        var bind = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .FirstOrDefault(b => b.Source.Name == "custom-orders" && b.Destination.Name == queueName);

        // assert
        Assert.NotNull(bind);
        Assert.Empty(bind.RoutingKey);
    }

    [Fact]
    public void DiscoverTopology_Should_ResolveProducerAndConsumerToSameEntity_When_ConventionNaming()
    {
        // arrange
        // with convention naming the producer's publish exchange and the exchange chain the consumer
        // binds into its queue must be one and the same entity, so the two paths cannot drift apart.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.BindHandlersImplicitly());
        var (topology, queueName) = ResolveConsumerQueue(runtime);
        var producerExchange = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var publishEndpoint = (RabbitMQDispatchEndpoint)runtime.GetPublishEndpoint(
            runtime.GetMessageType(typeof(OrderCreated)));
        var chainReachesQueue = topology.Bindings
            .OfType<RabbitMQQueueBinding>()
            .Any(b => b.Destination.Name == queueName);

        // assert
        // the producer targets the convention publish exchange and the consumer's chain binds the same
        // exchange entity into its queue, so the two paths converge on one entity.
        Assert.Equal(producerExchange, publishEndpoint.Exchange?.Name);
        Assert.NotNull(topology.Exchanges.FirstOrDefault(e => e.Name == producerExchange));
        Assert.True(chainReachesQueue);
    }

    [Fact]
    public void DiscoverTopology_Should_FailBuild_When_ConsumedTypeHasPerMessageRoutingKey()
    {
        // arrange
        // a per-message routing-key function makes the consume bind underivable, so the build must fail
        // rather than generate a key-less bind that may silently fail to match.
        // act
        var ex = Record.Exception(() => CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d => d.UseRabbitMQRoutingKey<OrderCreated>(m => m.OrderId));
            },
            t => t.BindHandlersImplicitly()));

        // assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains(typeof(OrderCreated).FullName!, ex.Message);
    }

    [Fact]
    public void DiscoverTopology_Should_FailBuild_When_ConsumedTypeHasExplicitQueueDestination()
    {
        // arrange
        // an explicit queue destination on a consumed type has no exchange chain to bind into the
        // endpoint queue, so the build must fail instead of guessing.
        // act
        var ex = Record.Exception(() => CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToRabbitMQQueue("orders-queue")));
            },
            t => t.BindHandlersImplicitly()));

        // assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains(typeof(OrderCreated).FullName!, ex.Message);
    }

    [Fact]
    public void DiscoverTopology_Should_NotConsultResolver_When_RouteIsReply()
    {
        // arrange
        // a saga with an OnReply transition registers a reply route. The convention must skip reply
        // routes so no chain appears for the reply type, even though the type has no derivable bind.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<StockCheckSaga>();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersImplicitly();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        var replyExchange = runtime.Naming.GetPublishEndpointName(typeof(StockInfoResult));

        // act
        var replyEntities = topology.Exchanges.Count(e => e.Name == replyExchange);

        // assert
        Assert.Equal(0, replyEntities);
    }

    [Fact]
    public void DiscoverTopology_Should_SuppressQueueBind_When_QueueAutoBindFalse()
    {
        // arrange
        // queue-scope auto-binding is off, so the convention must not bind any exchange into the
        // queue; the type-owned publish/send exchanges are kept because they belong to the type.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders").Queue("orders").Consumer<OrderSpyConsumer>().AutoBind(false);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_SuppressQueueBind_When_TypeAutoBindFalse()
    {
        // arrange
        // queue-scope auto-binding stays on but the per-type override turns it off for OrderCreated,
        // so its bind into the queue is suppressed while the convention exchanges remain.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders")
                    .Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .Receives<OrderCreated>(r => r.AutoBind(false));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_KeepQueueBind_When_TypeAutoBindTrueOverridesQueueFalse()
    {
        // arrange
        // the queue turns auto-binding off in bulk, but the per-type override re-enables it for
        // OrderCreated, so the convention binds the chain into the queue for that type (type > queue).
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("orders")
                    .Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .AutoBind(false)
                    .Receives<OrderCreated>(r => r.AutoBind(true));
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_OmitConventionChain_When_RouteIsReply()
    {
        // arrange
        // a saga with an OnReply transition registers a reply route; the convention skips reply routes,
        // so no convention chain appears for the reply type even though it has no derivable bind.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<StockCheckSaga>();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersImplicitly();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_KeepTypeExchanges_When_TransportAutoBindFalse()
    {
        // arrange
        // transport-scope auto-binding is off, so no exchange is bound into the queue; the type-owned
        // publish/send exchanges still appear because suppression scope removes only the queue binds.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoBind(false);
                t.BindHandlersExplicitly();
                t.Endpoint("orders").Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    private static (RabbitMQMessagingTopology Topology, string QueueName) ResolveConsumerQueue(
        MessagingRuntime runtime)
    {
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .First(e => e.Kind == ReceiveEndpointKind.Default);
        return ((RabbitMQMessagingTopology)transport.Topology, endpoint.Queue.Name);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
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

    public sealed class StockCheckStarted;

    public sealed class StockInfoResult;

    public sealed class StockCheckState : SagaStateBase;

    public sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

    public sealed class StockCheckSaga : Saga<StockCheckState>
    {
        protected override void Configure(ISagaDescriptor<StockCheckState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StockCheckStarted>()
                .StateFactory(_ => new StockCheckState())
                .Send((_, _) => new GetStockInfoRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<StockInfoResult>().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
