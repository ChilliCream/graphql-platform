using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class EndpointMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public EndpointMiddlewareTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UseReceive_Should_InvokeMiddleware_When_MessageReceived()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<TrackingConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("ep")
                    .Consumer<TrackingConsumer>()
                    .UseReceive(
                        new ReceiveMiddlewareConfiguration(
                            (_, next) =>
                                async context =>
                                {
                                    tracker.Add("receive-mw");
                                    await next(context);
                                },
                            "test-receive"));
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-MW" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("receive-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseReceive_After_Should_InvokeMiddleware_When_MessageReceived()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<TrackingConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("ep")
                    .Consumer<TrackingConsumer>()
                    .UseReceive(
                        new ReceiveMiddlewareConfiguration(
                            (_, next) =>
                                async context =>
                                {
                                    tracker.Add("append-receive-mw");
                                    await next(context);
                                },
                            "test-append-receive"),
                        after: "RabbitMQAcknowledgement");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-MW2" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("append-receive-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseReceive_Before_Should_InvokeMiddleware_When_MessageReceived()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<TrackingConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("ep")
                    .Consumer<TrackingConsumer>()
                    .UseReceive(
                        new ReceiveMiddlewareConfiguration(
                            (_, next) =>
                                async context =>
                                {
                                    tracker.Add("prepend-receive-mw");
                                    await next(context);
                                },
                            "test-prepend-receive"),
                        before: "RabbitMQAcknowledgement");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-MW3" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Consumer did not receive the message within timeout");
        Assert.Contains("prepend-receive-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Should_InvokeMiddleware_When_MessageDispatched()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<PaymentSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("ep")
                    .Publish<ProcessPayment>()
                    .UseDispatch(
                        new DispatchMiddlewareConfiguration(
                            (_, next) =>
                                async context =>
                                {
                                    tracker.Add("dispatch-mw");
                                    await next(context);
                                },
                            "test-dispatch"));
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new ProcessPayment { OrderId = "ORD-DM", Amount = 50.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("dispatch-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_After_Should_InvokeMiddleware_When_MessageDispatched()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<PaymentSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("ep")
                    .Publish<ProcessPayment>()
                    .UseDispatch(
                        new DispatchMiddlewareConfiguration(
                            (_, next) =>
                                async context =>
                                {
                                    tracker.Add("append-dispatch-mw");
                                    await next(context);
                                },
                            "test-append-dispatch"),
                        after: "Instrumentation");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new ProcessPayment { OrderId = "ORD-DM2", Amount = 25.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("append-dispatch-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Before_Should_InvokeMiddleware_When_MessageDispatched()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<PaymentSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("ep")
                    .Publish<ProcessPayment>()
                    .UseDispatch(
                        new DispatchMiddlewareConfiguration(
                            (ctx, next) =>
                                async context =>
                                {
                                    tracker.Add("prepend-dispatch-mw");
                                    await next(context);
                                },
                            "test-prepend-dispatch"),
                        before: "Instrumentation");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new ProcessPayment { OrderId = "ORD-DM3", Amount = 10.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("prepend-dispatch-mw", tracker.Invocations);
    }

    public sealed class MiddlewareTracker
    {
        public ConcurrentBag<string> Invocations { get; } = [];

        public void Add(string name) => Invocations.Add(name);
    }

    public sealed class TrackingConsumer(MessageRecorder recorder) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }

    public sealed class PaymentSpyConsumer(MessageRecorder recorder) : IConsumer<ProcessPayment>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ProcessPayment> context)
        {
            recorder.Record(context.Message);
            return default;
        }
    }
}
