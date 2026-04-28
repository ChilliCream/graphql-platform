using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

[Collection("EventHub")]
public class TransportMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly EventHubFixture _fixture;

    public TransportMiddlewareTests(EventHubFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UseReceive_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-receive-mw");
                                await next(context);
                            },
                        "test-transport-receive"))
                .Endpoint(hubName)
                    .Consumer<SpyConsumer>()
                    .ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-T1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("transport-receive-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseReceive_After_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-append-receive");
                                await next(context);
                            },
                        "test-transport-append-receive"),
                    after: "EventHubAcknowledgement")
                .Endpoint(hubName)
                    .Consumer<SpyConsumer>()
                    .ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-T2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("transport-append-receive", tracker.Invocations);
    }

    [Fact]
    public async Task UseReceive_Before_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-prepend-receive");
                                await next(context);
                            },
                        "test-transport-prepend-receive"),
                    before: "EventHubAcknowledgement")
                .Endpoint(hubName)
                    .Consumer<SpyConsumer>()
                    .ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-T3" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("transport-prepend-receive", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-dispatch-mw");
                                await next(context);
                            },
                        "test-transport-dispatch"))
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-TD1", Amount = 50.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("transport-dispatch-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_After_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-append-dispatch");
                                await next(context);
                            },
                        "test-transport-append-dispatch"),
                    after: "Instrumentation")
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-TD2", Amount = 25.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("transport-append-dispatch", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Before_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        var hubName = _fixture.GetHubForTest("middleware");
        var consumerGroup = _fixture.GetUniqueConsumerGroup();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddEventHub(t => t
                .ConnectionString(_fixture.ConnectionString)
                .UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-prepend-dispatch");
                                await next(context);
                            },
                        "test-transport-prepend-dispatch"),
                    before: "Instrumentation")
                .Endpoint(hubName).ConsumerGroup(consumerGroup))
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-TD3", Amount = 10.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("transport-prepend-dispatch", tracker.Invocations);
    }

    public sealed class MiddlewareTracker
    {
        public ConcurrentBag<string> Invocations { get; } = [];

        public void Add(string name) => Invocations.Add(name);
    }

    public sealed class SpyConsumer(MessageRecorder recorder) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }

    public sealed class PaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
