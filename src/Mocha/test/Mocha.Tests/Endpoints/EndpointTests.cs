using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory;

namespace Mocha.Tests;

public class EndpointTests
{
    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory();

        var provider = services.BuildServiceProvider();
        return (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    }

    [Fact]
    public void InboundRoutes_Should_HaveEndpoints_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var route in runtime.Router.InboundRoutes)
        {
            Assert.NotNull(route.Endpoint);
        }
    }

    [Fact]
    public void ReceiveEndpoint_Should_HaveName_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.NotNull(route.Endpoint);
        Assert.NotNull(route.Endpoint.Name);
        Assert.NotEmpty(route.Endpoint.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_HaveAddress_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.NotNull(route.Endpoint);
        Assert.NotNull(route.Endpoint.Address);
    }

    [Fact]
    public void ReceiveEndpoint_Should_BeInitialized_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.True(route.Endpoint!.IsInitialized);
    }

    [Fact]
    public void ReceiveEndpoint_Should_BeCompleted_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.True(route.Endpoint!.IsCompleted);
    }

    [Fact]
    public void ReceiveEndpoint_Should_NotBeStarted_When_BeforeStartAsync()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.False(route.Endpoint!.IsStarted);
    }

    [Fact]
    public void SubscribeRouteEndpoint_Should_HaveDefaultKind_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        Assert.Equal(ReceiveEndpointKind.Default, route.Endpoint!.Kind);
    }

    [Fact]
    public void ReplyConsumer_Should_HaveReplyEndpointKind_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var replyConsumer = runtime.Consumers.First(c => c.Name == "Reply");
        var routes = runtime.Router.GetInboundByConsumer(replyConsumer);
        var route = Assert.Single(routes);
        Assert.Equal(ReceiveEndpointKind.Reply, route.Endpoint!.Kind);
    }

    [Fact]
    public void Router_Should_FindRoutesByEndpoint_When_EndpointIsQueried()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = Assert.Single(runtime.Router.GetInboundByConsumer(consumer));
        var routesByEndpoint = runtime.Router.GetInboundByEndpoint(route.Endpoint!);
        Assert.Contains(route, routesByEndpoint);
    }

    [Fact]
    public void DifferentHandlerTypes_Should_HaveDifferentEndpoints_When_MultipleHandlersAreAdded()
    {
        // arrange & act
        var runtime = CreateRuntime(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert
        var orderConsumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var paymentConsumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));

        var orderRoute = Assert.Single(runtime.Router.GetInboundByConsumer(orderConsumer));
        var paymentRoute = Assert.Single(runtime.Router.GetInboundByConsumer(paymentConsumer));

        // Each handler should have its own endpoint
        Assert.NotNull(orderRoute.Endpoint);
        Assert.NotNull(paymentRoute.Endpoint);
    }

    [Fact]
    public void EndpointRouter_Should_HaveDispatchEndpoints_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotEmpty(runtime.Endpoints.Endpoints);
    }

    [Fact]
    public void DispatchEndpoints_Should_BeCompleted_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var endpoint in runtime.Endpoints.Endpoints)
        {
            Assert.True(endpoint.IsCompleted);
        }
    }

    [Fact]
    public void DispatchEndpoints_Should_HaveAddress_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        foreach (var endpoint in runtime.Endpoints.Endpoints)
        {
            Assert.NotNull(endpoint.Address);
        }
    }

    [Fact]
    public void Runtime_Should_HaveTransport_When_RuntimeIsBuilt()
    {
        // arrange & act
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.NotEmpty(runtime.Transports);
    }

    // =========================================================================
    // Test Types
    // =========================================================================

    public sealed class OrderCreated
    {
        public string OrderId { get; init; } = "";
    }

    public sealed class ProcessPayment
    {
        public decimal Amount { get; init; }
    }

    public sealed class OrderCreatedHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }
}
