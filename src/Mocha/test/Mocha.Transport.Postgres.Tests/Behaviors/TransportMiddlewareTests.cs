using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Behaviors;

[Collection("Postgres")]
public class TransportMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly PostgresFixture _fixture;

    public TransportMiddlewareTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UseReceive_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-receive-mw");
                                await next(context);
                            },
                        "test-transport-receive"));

                t.DeclareTopic("ex");
                t.DeclareQueue("q");
                t.DeclareSubscription("ex", "q");
                t.Queue("q").Consumer<SpyConsumer>();
                t.DispatchEndpoint("dispatch").ToTopic("ex").Publish<OrderCreated>();
            })
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-append-receive");
                                await next(context);
                            },
                        "test-transport-append-receive"),
                    after: "PostgresParsing");

                t.DeclareTopic("ex");
                t.DeclareQueue("q");
                t.DeclareSubscription("ex", "q");
                t.Queue("q").Consumer<SpyConsumer>();
                t.DispatchEndpoint("dispatch").ToTopic("ex").Publish<OrderCreated>();
            })
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddConsumer<SpyConsumer>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-prepend-receive");
                                await next(context);
                            },
                        "test-transport-prepend-receive"),
                    before: "PostgresParsing");

                t.DeclareTopic("ex");
                t.DeclareQueue("q");
                t.DeclareSubscription("ex", "q");
                t.Queue("q").Consumer<SpyConsumer>();
                t.DispatchEndpoint("dispatch").ToTopic("ex").Publish<OrderCreated>();
            })
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-dispatch-mw");
                                await next(context);
                            },
                        "test-transport-dispatch"));
            })
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
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseDispatch(CreateDispatchAnchor(tracker));
                t.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-append-dispatch:before");
                                await next(context);
                                tracker.Add("transport-append-dispatch:after");
                            },
                        "test-transport-append-dispatch"),
                    after: "DispatchAnchor");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-TD2", Amount = 25.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Equal(
            ["anchor:before", "transport-append-dispatch:before", "transport-append-dispatch:after", "anchor:after"],
            tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Before_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var db = await _fixture.CreateDatabaseAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddPostgres(t =>
            {
                t.ConnectionString(db.ConnectionString);

                t.UseDispatch(CreateDispatchAnchor(tracker));
                t.UseDispatch(
                    new DispatchMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-prepend-dispatch:before");
                                await next(context);
                                tracker.Add("transport-prepend-dispatch:after");
                            },
                        "test-transport-prepend-dispatch"),
                    before: "DispatchAnchor");
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(new ProcessPayment { OrderId = "ORD-TD3", Amount = 10.00m }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Equal(
            ["transport-prepend-dispatch:before", "anchor:before", "anchor:after", "transport-prepend-dispatch:after"],
            tracker.Invocations);
    }

    private static DispatchMiddlewareConfiguration CreateDispatchAnchor(MiddlewareTracker tracker)
        => new(
            (_, next) => async context =>
            {
                tracker.Add("anchor:before");
                await next(context);
                tracker.Add("anchor:after");
            },
            "DispatchAnchor");

    public sealed class MiddlewareTracker
    {
        public ConcurrentQueue<string> Invocations { get; } = new();

        public void Add(string name) => Invocations.Enqueue(name);
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
