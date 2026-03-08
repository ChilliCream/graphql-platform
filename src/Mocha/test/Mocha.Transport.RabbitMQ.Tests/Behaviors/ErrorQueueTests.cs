using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public class ErrorQueueTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private readonly RabbitMQFixture _fixture;

    public ErrorQueueTests(RabbitMQFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PublishAsync_Should_RouteToErrorQueue_When_HandlerThrows()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("handler-ep").Handler<ThrowingOrderHandler>().FaultEndpoint("rabbitmq:///q/handler-q_error");
                t.Endpoint("error-ep")
                    .Queue("handler-q_error")
                    // we mark it as an error because only then no route will be provisoned for the
                    // spy (otherwise the normal order hanlder publish will also go to the spy)
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<ErrorSpyConsumer>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-FAULT" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Error queue consumer did not receive the faulted message");

        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.ContainsKey("fault-exception-type"), "Missing fault-exception-type header");
        Assert.True(headers.ContainsKey("fault-message"), "Missing fault-message header");
        Assert.True(headers.ContainsKey("fault-stack-trace"), "Missing fault-stack-trace header");
        Assert.True(headers.ContainsKey("fault-timestamp"), "Missing fault-timestamp header");
    }

    [Fact]
    public async Task SendAsync_Should_RouteToErrorQueue_When_HandlerThrows()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddRequestHandler<ThrowingPaymentHandler>()
            .AddConsumer<PaymentErrorSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("payment-ep")
                    .Handler<ThrowingPaymentHandler>()
                    .FaultEndpoint("rabbitmq:///q/payment-q_error");
                t.Endpoint("payment-error-ep")
                    .Queue("payment-q_error")
                    // we mark it as an error because only then no route will be provisoned for the
                    // spy (otherwise the normal order hanlder publish will also go to the spy)
                    .Kind(ReceiveEndpointKind.Error)
                    .Consumer<PaymentErrorSpyConsumer>();
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.SendAsync(
            new ProcessPayment { OrderId = "PAY-FAULT", Amount = 99.99m },
            CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Error queue consumer did not receive the faulted message");

        var headers = Assert.Single(capture.CapturedHeaders);
        Assert.True(headers.ContainsKey("fault-exception-type"), "Missing fault-exception-type header");
        Assert.True(headers.ContainsKey("fault-message"), "Missing fault-message header");
    }

    [Fact]
    public async Task ErrorQueue_Should_PreserveOriginalBody_When_HandlerFaults()
    {
        // arrange
        var capture = new ErrorCapture();
        await using var vhost = await _fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(capture)
            .AddMessageBus()
            .AddEventHandler<ThrowingOrderHandler>()
            .AddConsumer<ErrorSpyConsumer>()
            .AddRabbitMQ(t =>
            {
                t.Endpoint("handler-ep").Handler<ThrowingOrderHandler>().FaultEndpoint("rabbitmq:///q/handler-q_error");
                t.Endpoint("error-ep")
                    .Consumer<ErrorSpyConsumer>()
                    .Queue("handler-q_error")
                    // we mark it as an error because only then no route will be provisoned for the
                    // spy (otherwise the normal order hanlder publish will also go to the spy)
                    .Kind(ReceiveEndpointKind.Error);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(new OrderCreated { OrderId = "ORD-PRESERVE" }, CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout), "Error queue consumer did not receive the faulted message");

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

        public void RecordHeaders(IConsumeContext context)
        {
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

    public sealed class PaymentErrorSpyConsumer(ErrorCapture capture) : IConsumer<ProcessPayment>
    {
        public ValueTask ConsumeAsync(IConsumeContext<ProcessPayment> context)
        {
            capture.RecordHeaders(context);
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

    public sealed class ThrowingPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Payment handler failed deliberately");
        }
    }
}
