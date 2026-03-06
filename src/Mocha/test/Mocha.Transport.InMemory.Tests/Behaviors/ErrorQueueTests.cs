using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class ErrorQueueTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_RouteToErrorQueue_When_HandlerThrows()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddInMemory(t =>
            {
                t.Endpoint("handler-ep")
                    .Handler<ThrowingOrderHandler>()
                    .Queue("handler-q")
                    .FaultEndpoint("memory:///q/handler-q_error");

                t.Endpoint("error-ep")
                    .Consumer<ErrorSpyConsumer>()
                    .Queue("handler-q_error")
                    .Kind(ReceiveEndpointKind.Error);
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-FAULT" }, CancellationToken.None);

        // assert — faulted message lands in error queue with fault headers
        Assert.True(await capture.WaitAsync(s_timeout), "Error consumer did not receive the faulted message");
        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.TryGetValue("fault-exception-type", out var exType));
        Assert.Contains("InvalidOperationException", (string?)exType);
        Assert.True(headers.TryGetValue("fault-message", out var faultMsg));
        Assert.Equal("Handler failed deliberately", (string?)faultMsg);
        Assert.True(headers.ContainsKey("fault-stack-trace"));
        Assert.True(headers.ContainsKey("fault-timestamp"));
    }

    [Fact]
    public async Task SendAsync_Should_RouteToErrorQueue_When_HandlerThrows()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddRequestHandler<ThrowingPaymentHandler>()
            .AddConsumer<ErrorSpySendConsumer>()
            .AddInMemory(t =>
            {
                t.Endpoint("payment-ep")
                    .Handler<ThrowingPaymentHandler>()
                    .Queue("payment-q")
                    .FaultEndpoint("memory:///q/payment-q_error");

                t.Endpoint("error-ep")
                    .Consumer<ErrorSpySendConsumer>()
                    .Queue("payment-q_error")
                    .Kind(ReceiveEndpointKind.Error);

                t.DispatchEndpoint("payment-dispatch").ToQueue("payment-q").Send<ProcessPayment>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.SendAsync(new ProcessPayment { OrderId = "ORD-FAULT", Amount = 10.00m }, CancellationToken.None);

        // assert — faulted message lands in error queue
        Assert.True(await capture.WaitAsync(s_timeout), "Error consumer did not receive the faulted message");
        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.TryGetValue("fault-exception-type", out var exType));
        Assert.Contains("InvalidOperationException", (string?)exType);
    }

    [Fact]
    public async Task ErrorQueue_Should_PreserveOriginalBody_When_HandlerFaults()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddInMemory(t =>
            {
                t.Endpoint("handler-ep")
                    .Handler<ThrowingOrderHandler>()
                    .Queue("handler-q")
                    .FaultEndpoint("memory:///q/handler-q_error");

                t.Endpoint("error-ep")
                    .Consumer<ErrorSpyConsumer>()
                    .Queue("handler-q_error")
                    .Kind(ReceiveEndpointKind.Error);
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "ORD-PRESERVE" }, CancellationToken.None);

        // assert — error queue consumer receives the original message
        Assert.True(await capture.WaitAsync(s_timeout), "Error consumer did not receive the faulted message");
        var msg = Assert.Single(capture.Messages);
        Assert.Equal("ORD-PRESERVE", msg.OrderId);
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

        public void RecordHeaders(IReadOnlyHeaders headers)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var h in headers)
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

    public sealed class ErrorSpySendConsumer(ErrorCapture capture) : IConsumer<ProcessPayment>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ProcessPayment> context)
        {
            capture.RecordHeaders(context.Headers);
            return default;
        }
    }

    public sealed class ThrowingOrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }

    public sealed class ThrowingPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Handler failed deliberately");
        }
    }
}
