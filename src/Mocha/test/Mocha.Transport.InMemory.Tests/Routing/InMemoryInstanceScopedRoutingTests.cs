using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Routing;

public class InMemoryInstanceScopedRoutingTests
{
    [Fact]
    public void InstanceScoped_Should_BindToTemporaryInstanceEndpoint_When_BindingImplicitly()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>(d => d.InstanceScoped())
            .AddInMemory()
            .BuildRuntime();

        // assert
        DescribeRoutes(runtime).MatchInlineSnapshot(
            """
            [
              "GetOrderStatusHandler -> get-order-status-{instance} (temporary: True)"
            ]
            """);
    }

    [Fact]
    public void InstanceScoped_Should_BindToTemporaryInstanceEndpoint_When_BindingExplicitly()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>(d => d.InstanceScoped())
            .AddInMemory(t => t.BindExplicitly())
            .BuildRuntime();

        // assert
        DescribeRoutes(runtime).MatchInlineSnapshot(
            """
            [
              "GetOrderStatusHandler -> get-order-status-{instance} (temporary: True)"
            ]
            """);
    }

    [Fact]
    public void InstanceScoped_Should_NotClaimMessageType_When_SharedHandlerHandlesSameRequest()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>()
            .AddRequestHandler<InstanceOrderStatusHandler>(d => d.InstanceScoped())
            .AddInMemory()
            .BuildRuntime();

        // assert
        DescribeRoutes(runtime).MatchInlineSnapshot(
            """
            [
              "GetOrderStatusHandler -> get-order-status (temporary: False)",
              "InstanceOrderStatusHandler -> get-order-status-{instance} (temporary: True)"
            ]
            """);
    }

    [Fact]
    public async Task RequestAsync_Should_ReachInstanceScopedHandler_When_SentToInstanceEndpoint()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddMessageBus()
            .AddRequestHandler<GetOrderStatusHandler>(d => d.InstanceScoped())
            .AddInMemory();
        await using var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        var address = runtime.Router.InboundRoutes.Single(r => r.IsInstanceScoped).Endpoint!.Source.Address;

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(
            new GetOrderStatus { OrderId = "1" },
            new SendOptions { Endpoint = address },
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("Shipped", response.Status);
    }

    private static string[] DescribeRoutes(MessagingRuntime runtime)
    {
        var instanceId = runtime.Host.InstanceId.ToString("N");
        return
        [
            .. runtime.Router.InboundRoutes
                .Where(r => r.Kind != InboundRouteKind.Reply)
                .Select(r =>
                {
                    var endpoint = (InMemoryReceiveEndpoint)r.Endpoint!;
                    var name = endpoint.Name.Replace(instanceId, "{instance}");
                    return $"{r.Consumer!.Name} -> {name} (temporary: {endpoint.Configuration.IsTemporary})";
                })
                .Order(StringComparer.Ordinal)
        ];
    }

    public sealed class InstanceOrderStatusHandler : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
    {
        public ValueTask<OrderStatusResponse> HandleAsync(
            GetOrderStatus request,
            CancellationToken cancellationToken)
            => new(new OrderStatusResponse { OrderId = request.OrderId, Status = "Instance" });
    }
}
