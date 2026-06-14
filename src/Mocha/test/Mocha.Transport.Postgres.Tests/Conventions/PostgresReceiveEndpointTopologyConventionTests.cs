using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Conventions;

/// <summary>
/// Verifies that <see cref="PostgresReceiveEndpointTopologyConvention"/> respects per-route
/// auto-binding gating and the P1 reply-route guard: subscriptions are suppressed when
/// auto-binding is off while type-owned topics remain, and reply routes never produce
/// convention topics or subscriptions regardless of auto-binding state.
/// </summary>
public class PostgresReceiveEndpointTopologyConventionTests
{
    [Fact]
    public void DiscoverTopology_Should_KeepConventionTopics_When_ExplicitBindingAndAutoBindDefaultOn()
    {
        // arrange
        // explicit binding places the handler but auto-binding is on by default; the convention
        // still creates the publish and send topics for the handled type because auto-binding
        // governs the subscription into the queue, not the type-owned topic entities.
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var conventionTopics = topology.Topics
            .Where(t => t.Name.Contains("order-created"))
            .ToList();

        // assert
        // the explicit binding places the handler on an endpoint; with auto-binding on, the
        // convention creates the publish/send topics so a producer can still reach the queue.
        Assert.NotEmpty(conventionTopics);
    }

    [Fact]
    public void DiscoverTopology_Should_OmitConventionSubscription_When_SagaHasOnReplyTransition()
    {
        // arrange
        // A saga with an OnReply transition registers an InboundRouteKind.Reply route. The receive
        // convention must skip reply routes so no spurious topic or subscription appears for the reply type.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderStockCheckSaga>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.BindHandlersImplicitly();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act
        var replyTopics = topology.Topics
            .Where(t => t.Name.Contains("stock-info-result"))
            .ToList();
        var replySubscriptions = topology.Subscriptions
            .Where(s => s.Source.Name.Contains("stock-info-result"))
            .ToList();

        // assert
        // Only topics for the start event appear. No topic or subscription for
        // StockInfoResult (the reply type) should be present.
        Assert.Empty(replyTopics);
        Assert.Empty(replySubscriptions);
    }

    [Fact]
    public void DiscoverTopology_Should_SuppressSubscription_When_QueueAutoBindFalse()
    {
        // arrange
        // queue-scope auto-binding is off, so the convention must not create any subscription into
        // the queue; the type-owned publish and send topics are kept because they belong to the type.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>().AutoBind(false);
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_KeepTopics_When_TransportAutoBindFalse()
    {
        // arrange
        // transport-scope auto-binding is off, so no subscription is created into the queue;
        // the type-owned publish and send topics still appear because suppression scope removes
        // only the queue subscriptions, not the type-owned topic entities.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.AutoBind(false);
                t.BindHandlersExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_OmitConventionChain_When_RouteIsReply()
    {
        // arrange
        // a saga with an OnReply transition registers a reply route; the convention skips reply
        // routes so no topic or subscription appears for the reply type even with auto-binding on.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderStockCheckSaga>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.BindHandlersImplicitly();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var replyTopicName = runtime.Naming.GetPublishEndpointName(typeof(StockInfoResult));

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
        Assert.DoesNotContain(replyTopicName, description.Topology?.Entities.Select(e => e.Name) ?? []);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IPostgresMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
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

    public sealed class StockCheckStarted;

    public sealed class StockInfoResult;

    public sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

    public sealed class StockCheckState : SagaStateBase;

    public sealed class OrderStockCheckSaga : Saga<StockCheckState>
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
