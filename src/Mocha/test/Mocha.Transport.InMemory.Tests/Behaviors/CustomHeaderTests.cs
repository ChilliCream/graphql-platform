using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests.Behaviors;

public class CustomHeaderTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task PublishAsync_Should_PropagateHeaders_When_CustomHeadersSet()
    {
        // arrange — register handler + IConsumer wiretap; both receive via fan-out
        var recorder = new MessageRecorder();
        var capture = new HeaderCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddConsumer<HeaderSpyConsumer>()
            .AddInMemory()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(
            new OrderCreated { OrderId = "ORD-HDR" },
            new PublishOptions
            {
                Headers = new() { ["x-tenant"] = "acme", ["x-trace-id"] = "trace-123" }
            },
            CancellationToken.None);

        // assert — consumer wiretap receives headers through the pipeline
        Assert.True(await capture.WaitAsync(s_timeout), "Consumer did not receive the event within timeout");
        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.TryGetValue("x-tenant", out var tenant), "Custom header 'x-tenant' not found");
        Assert.Equal("acme", tenant);
        Assert.True(headers.TryGetValue("x-trace-id", out var traceId), "Custom header 'x-trace-id' not found");
        Assert.Equal("trace-123", traceId);
    }

    [Fact]
    public async Task SendAsync_Should_PropagateHeaders_When_CustomHeadersSet()
    {
        // arrange — register throwing handler + IConsumer on error queue to capture headers
        var capture = new HeaderCapture();
        await using var provider = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddRequestHandler<ThrowingPaymentHandler>()
            .AddConsumer<PaymentHeaderSpyConsumer>()
            .AddInMemory(t =>
            {
                t.Endpoint("payment-ep")
                    .Handler<ThrowingPaymentHandler>()
                    .Queue("payment-q")
                    .FaultEndpoint("memory:///q/payment-q_error");

                t.Endpoint("error-ep")
                    .Consumer<PaymentHeaderSpyConsumer>()
                    .Queue("payment-q_error")
                    .Kind(ReceiveEndpointKind.Error);

                t.DispatchEndpoint("payment-dispatch").ToQueue("payment-q").Send<ProcessPayment>();
            })
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act — send with custom headers; handler throws so message routes to error queue
        await bus.SendAsync(
            new ProcessPayment { OrderId = "ORD-HDR", Amount = 10.00m },
            new SendOptions { Headers = new() { ["x-tenant"] = "acme" } },
            CancellationToken.None);

        // assert — error queue consumer preserves custom headers alongside fault headers
        Assert.True(await capture.WaitAsync(s_timeout), "Error consumer did not receive the faulted message");
        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(
            headers.TryGetValue("x-tenant", out var tenant),
            "Custom header 'x-tenant' not found on error queue envelope");
        Assert.Equal("acme", tenant);

        // Fault headers also present
        Assert.True(headers.ContainsKey("fault-exception-type"));
    }

    public sealed class HeaderCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

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

    public sealed class HeaderSpyConsumer(HeaderCapture capture) : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context)
        {
            capture.RecordHeaders(context.Headers);
            return default;
        }
    }

    public sealed class PaymentHeaderSpyConsumer(HeaderCapture capture) : IConsumer<ProcessPayment>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ProcessPayment> context)
        {
            capture.RecordHeaders(context.Headers);
            return default;
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
