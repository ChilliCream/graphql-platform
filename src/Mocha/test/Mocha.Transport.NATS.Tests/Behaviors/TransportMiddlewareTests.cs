using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class TransportMiddlewareTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly NatsFixture _fixture;

    public TransportMiddlewareTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task UseReceive_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddNats(t =>
            {
                t.UseReceive(
                    new ReceiveMiddlewareConfiguration(
                        (ctx, next) =>
                            async context =>
                            {
                                tracker.Add("transport-receive-mw");
                                await next(context);
                            },
                        "test-transport-receive"));
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-T1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(s_timeout), "Handler did not receive the message within timeout");
        Assert.Contains("transport-receive-mw", tracker.Invocations);
    }

    [Fact]
    public async Task UseDispatch_Should_InvokeOnAllEndpoints_When_ConfiguredAtTransportLevel()
    {
        // arrange
        var tracker = new MiddlewareTracker();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(tracker)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddNats(t =>
            {
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

    public sealed class MiddlewareTracker
    {
        public ConcurrentBag<string> Invocations { get; } = [];

        public void Add(string name) => Invocations.Add(name);
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
