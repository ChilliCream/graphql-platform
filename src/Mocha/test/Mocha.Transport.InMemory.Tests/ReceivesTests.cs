using Microsoft.Extensions.DependencyInjection;
using Mocha.TestHelpers;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class ReceivesTests
{
    [Fact]
    public void Receives_Should_BindHandlerToEndpoint_When_MessageTypeDeclared()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Queue("orders")
                    .Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "orders");

        Assert.NotNull(endpoint);
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.All(routes, r => Assert.Equal(endpoint, r.Endpoint));
    }

    [Fact]
    public void Receives_Should_BindAllHandlers_When_MultipleHandlersForSameType()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHandler<OrderCreatedHandler2>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Queue("orders")
                    .Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "orders");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.All(routes, r => Assert.Equal(endpoint, r.Endpoint));
    }

    [Fact]
    public void Receives_Should_FanOutToBothQueues_When_SameTypeDeclaredOnTwoEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders-primary");
                t.DeclareQueue("orders-backup");
                t.Endpoint("primary").Queue("orders-primary")
                    .Receives<OrderCreated>();
                t.Endpoint("backup").Queue("orders-backup")
                    .Receives<OrderCreated>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var primaryEndpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "primary");
        var backupEndpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "backup");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, r => r.Endpoint == primaryEndpoint);
        Assert.Contains(routes, r => r.Endpoint == backupEndpoint);
    }

    [Fact]
    public void Receives_Should_Throw_When_NoHandlerRegistered()
    {
        // arrange & act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection()
                .AddMessageBus()
                .AddInMemory(t =>
                {
                    t.BindHandlersExplicitly();
                    t.DeclareQueue("orders");
                    t.Endpoint("orders").Queue("orders")
                        .Receives<OrderCreated>();
                })
                .BuildRuntime());

        Assert.Contains("No handler or consumer handles message type", exception.Message);
        Assert.Contains(typeof(OrderCreated).FullName!, exception.Message);
        Assert.Contains("orders", exception.Message);
    }

    [Fact]
    public void Consumer_Should_BindToBothEndpoints_When_MappedToTwoEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders-a");
                t.DeclareQueue("orders-b");
                t.Endpoint("endpoint-a").Queue("orders-a")
                    .Consumer<TestOrderConsumer>();
                t.Endpoint("endpoint-b").Queue("orders-b")
                    .Consumer<TestOrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpointA = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "endpoint-a");
        var endpointB = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "endpoint-b");

        var routes = runtime.Router.InboundRoutes
            .Where(r => r.Consumer?.Identity == typeof(TestOrderConsumer))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Contains(routes, r => r.Endpoint == endpointA);
        Assert.Contains(routes, r => r.Endpoint == endpointB);
    }

    [Fact]
    public async Task Receives_Should_DeliverMessages_When_Published()
    {
        // arrange
        var recorder = new MessageRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var provider = await services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Queue("orders")
                    .Receives<OrderCreated>();
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "123" }, CancellationToken.None);
        var received = await recorder.WaitAsync(TimeSpan.FromSeconds(5));

        // assert
        Assert.True(received);
        Assert.Single(recorder.Messages);
        var message = Assert.IsType<OrderCreated>(recorder.Messages.First());
        Assert.Equal("123", message.OrderId);
    }

    [Fact]
    public void Receives_Should_NotCreateDuplicateRoutes_When_SameTypeDeclaredOnThreeEndpoints()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders-1");
                t.DeclareQueue("orders-2");
                t.DeclareQueue("orders-3");
                t.Endpoint("e1").Queue("orders-1").Receives<OrderCreated>();
                t.Endpoint("e2").Queue("orders-2").Receives<OrderCreated>();
                t.Endpoint("e3").Queue("orders-3").Receives<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(3, routes.Count);
        Assert.Equal(3, routes.Select(r => r.Endpoint).Distinct().Count());
    }

    [Fact]
    public void Receives_Should_PreserveCondition_When_FannedOut()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders-primary");
                t.DeclareQueue("orders-backup");
                t.Endpoint("primary").Queue("orders-primary").Receives<OrderCreated>();
                t.Endpoint("backup").Queue("orders-backup").Receives<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var routes = runtime.Router.InboundRoutes
            .Where(r => r.MessageType?.RuntimeType == typeof(OrderCreated))
            .ToList();
        Assert.Equal(2, routes.Count);
        Assert.Same(routes[0].Condition, routes[1].Condition);
    }

    [Fact]
    public async Task Receives_Should_DeliverToBothQueues_When_FannedOut()
    {
        // arrange
        var recorder = new MessageRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var provider = await services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders-primary");
                t.DeclareQueue("orders-backup");
                t.Endpoint("primary").Queue("orders-primary").Receives<OrderCreated>();
                t.Endpoint("backup").Queue("orders-backup").Receives<OrderCreated>();
            })
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "123" }, CancellationToken.None);
        var received = await recorder.WaitAsync(TimeSpan.FromSeconds(5), expectedCount: 2);

        // assert
        Assert.True(received);
        Assert.Equal(2, recorder.Messages.Count);
    }
}

public sealed class TestOrderConsumer : IConsumer<OrderCreated>
{
    public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
}
