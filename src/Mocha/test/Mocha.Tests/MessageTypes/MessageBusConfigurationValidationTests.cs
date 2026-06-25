using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.MessageTypes;

public sealed class MessageBusConfigurationValidationTests
{
    [Fact]
    public void Build_Should_ReportAllUnboundInboundRoutes_When_ExplicitBindLeavesHandlersUnbound()
    {
        var exception = BuildThrows(builder =>
        {
            builder.AddEventHandler<OrderCreatedHandler>();
            builder.AddEventHandler<ItemShippedHandler>();
            builder.AddInMemory(t => t.BindExplicitly());
        });

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void Build_Should_ReportAlsoBoundElsewhere_When_SameMessageHasAnotherBoundRoute()
    {
        var exception = BuildThrows(builder =>
        {
            builder.AddEventHandler<BoundOrderCreatedHandler>();
            builder.AddEventHandler<UnboundOrderCreatedHandler>();
            builder.AddInMemory(t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Handler<BoundOrderCreatedHandler>();
            });
        });

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void Build_Should_ReportNoTransportHint_When_HandlerRegisteredWithoutTransport()
    {
        var exception = BuildThrows(builder => builder.AddEventHandler<OrderCreatedHandler>());

        exception.Message.MatchSnapshot();
    }

    [Fact]
    public async Task Build_Should_BindSagaReplyRoutesToReplyEndpoint_When_ExplicitBindMode()
    {
        await InspectRuntimeAsync(
            builder =>
            {
                builder.AddSaga<StockCheckSaga>();
                builder.AddInMemory(t =>
                {
                    t.BindExplicitly();
                    t.Queue("stock-start").Receives<StockCheckStarted>();
                });
            },
            runtime =>
            {
                var sagaConsumer = runtime.Consumers.Single(consumer => consumer.Identity == typeof(StockCheckSaga));
                var replyRoute = runtime.Router
                    .GetInboundByConsumer(sagaConsumer)
                    .Single(route => route.Kind == InboundRouteKind.Reply);

                Assert.Equal(ReceiveEndpointKind.Reply, replyRoute.Endpoint?.Kind);

                Assert.All(
                    runtime.Router.InboundRoutes.Where(route => route.Kind == InboundRouteKind.Reply),
                    route => Assert.NotNull(route.Endpoint));
            services => services.AddInMemorySagas());
    }

    [Fact]
    public void Build_Should_NotUseReplyRouteAsDuplicate_When_NormalRouteWithSameMessageIsUnbound()
    {
        var exception = BuildThrows(
            builder =>
            {
                builder.AddSaga<StockCheckSaga>();
                builder.AddEventHandler<StockInfoResultHandler>();
                builder.AddInMemory(t =>
                {
                    t.BindExplicitly();
                    t.Queue("stock-start").Receives<StockCheckStarted>();
                });
            },
            services => services.AddInMemorySagas());

        exception.Message.MatchSnapshot();
    }

    private static InvalidOperationException BuildThrows(
        Action<IMessageBusHostBuilder> configure,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        var builder = services.AddMessageBus();
        configure(builder);

        using var provider = services.BuildServiceProvider();
        return Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IMessagingRuntime>());
    }

    private static async Task InspectRuntimeAsync(
        Action<IMessageBusHostBuilder> configure,
        Action<MessagingRuntime> inspect,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        configureServices?.Invoke(services);
        var builder = services.AddMessageBus();
        configure(builder);

        await using var provider = services.BuildServiceProvider();
        inspect(Assert.IsType<MessagingRuntime>(provider.GetRequiredService<IMessagingRuntime>()));
    }

    public sealed class OrderCreated;

    public sealed class ItemShipped;

    public sealed class StockCheckStarted;

    public sealed class StockInfoResult;

    public sealed class StockInfoResultHandler : IEventHandler<StockInfoResult>
    {
        public ValueTask HandleAsync(StockInfoResult message, CancellationToken cancellationToken) => default;
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ItemShippedHandler : IEventHandler<ItemShipped>
    {
        public ValueTask HandleAsync(ItemShipped message, CancellationToken cancellationToken) => default;
    }

    public sealed class BoundOrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class UnboundOrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

    public sealed class StockCheckState : SagaStateBase;

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

            descriptor
                .During("Awaiting")
                .OnReply<StockInfoResult>()
                .TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}
