using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Conventions;

/// <summary>
/// Verifies that <see cref="PostgresReceiveEndpointTopologyConvention"/> respects per-route
/// bind-mode gating and the P1 reply-route guard: subscriptions are suppressed when
/// bind mode is Explicit while type-owned topics remain, and reply routes never produce
/// convention topics or subscriptions regardless of bind mode.
/// </summary>
public class PostgresReceiveEndpointTopologyConventionTests
{
    [Fact]
    public void DiscoverTopology_Should_SuppressConventionSubscriptions_When_TransportBindExplicit()
    {
        // arrange
        // BindExplicitly suppresses both discovery and convention subscriptions.
        // Type-owned publish/send topics are still created (they survive regardless of bind mode)
        // but no subscription into the queue is generated.
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddPostgres(t =>
            {
                t.ConnectionString("Host=localhost;Database=mocha_test;Username=test;Password=test");
                t.BindExplicitly();
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
        // Type-owned topics remain even under BindExplicitly.
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
                t.BindImplicitly();
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
    public void DiscoverTopology_Should_SuppressSubscription_When_QueueBindExplicit()
    {
        // arrange
        // queue-scope auto-binding is off, so the convention must not create any subscription into
        // the queue; the type-owned publish and send topics are kept because they belong to the type.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>().BindExplicitly();
            });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert
        PostgresDescribeSnapshot.Create(description).MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_KeepTopics_When_TransportBindExplicit()
    {
        // arrange
        // transport-scope auto-binding is off, so no subscription is created into the queue;
        // the type-owned publish and send topics still appear because suppression scope removes
        // only the queue subscriptions, not the type-owned topic entities.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.BindExplicitly();
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
                t.BindImplicitly();
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
