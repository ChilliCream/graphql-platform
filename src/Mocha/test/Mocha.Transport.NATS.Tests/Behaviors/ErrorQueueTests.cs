using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.NATS.Tests.Helpers;
using NATS.Client.Core;

namespace Mocha.Transport.NATS.Tests.Behaviors;

[Collection("NATS")]
public class ErrorQueueTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly NatsFixture _fixture;

    public ErrorQueueTests(NatsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToErrorEndpoint_When_HandlerThrows()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddNats(t =>
            {
                t.Endpoint("handler-ep").Handler<ThrowingOrderHandler>().FaultEndpoint("nats:///s/handler-ep_error");
                t.Endpoint("error-ep")
                    .Subject("handler-ep_error")
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<ErrorSpyConsumer>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-FAULT" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Error endpoint consumer did not receive the faulted message");

        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.ContainsKey("fault-exception-type"), "Missing fault-exception-type header");
        Assert.True(headers.ContainsKey("fault-message"), "Missing fault-message header");
        Assert.True(headers.ContainsKey("fault-stack-trace"), "Missing fault-stack-trace header");
        Assert.True(headers.ContainsKey("fault-timestamp"), "Missing fault-timestamp header");
    }

    [Fact]
    public async Task ErrorEndpoint_Should_PreserveOriginalBody_When_HandlerFaults()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var bus = await new ServiceCollection()
            .AddSingleton(new NatsConnection(_fixture.CreateOptions()))
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddNats(t =>
            {
                t.Endpoint("handler-ep").Handler<ThrowingOrderHandler>().FaultEndpoint("nats:///s/handler-ep_error");
                t.Endpoint("error-ep")
                    .Consumer<ErrorSpyConsumer>()
                    .Subject("handler-ep_error")
                    .Kind(ReceiveEndpointKind.Error);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-PRESERVE" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Error endpoint consumer did not receive the faulted message");

        var message = Assert.Single(capture.Messages);
        Assert.Equal("ORD-PRESERVE", message.OrderId);
    }

    public sealed class ErrorCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<OrderCreated> Messages { get; } = [];
        public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

        public void Record(IConsumeContext<OrderCreated> context)
        {
            Messages.Add(context.Message);
            var dict = new Dictionary<string, object?>();
            foreach (var h in context.Headers)
            {
                dict[h.Key] = h.Value;
            }

            CapturedHeaders.Add(dict);
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ErrorSpyConsumer(ErrorCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.Record(context);
            return default;
        }
    }

    public sealed class ThrowingOrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Order handler failed deliberately");
        }
    }
}
