using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class PublishingReliabilityTests(RabbitMQFixture fixture)
{
    [Fact]
    public async Task SendAsync_Should_DeliverOnce_When_ConcurrentPublishTargetsMissingExchange()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("orders");
                t.DeclareQueue("healthy");
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
                t.DispatchEndpoint("healthy-send").ToQueue("healthy").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "warmup" });
        await SendAsync(bus, new ProcessPayment { OrderId = "warmup", Amount = 1 });
        var dispatcher = bus.Provider.GetRequiredService<IMessagingRuntime>()
            .Transports.OfType<RabbitMQMessagingTransport>().Single().Dispatcher;
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        dispatcher.Return(first, reusable: true);
        dispatcher.Return(second, reusable: true);
        await DeleteExchangeAsync(vhost, "orders");

        // act
        Task publish;
        Task send;
        await using (await vhost.Faults.FreezeConnectionsAsync())
        {
            publish = PublishAsync(bus, new OrderCreated { OrderId = "missing-exchange" });
            send = SendAsync(bus, new ProcessPayment { OrderId = "healthy", Amount = 1 });
        }

        var exception = await Assert.ThrowsAsync<AlreadyClosedException>(() => publish);
        await send;

        // assert
        Assert.Equal<ushort?>(Constants.NotFound, exception.ShutdownReason?.ReplyCode);
        Assert.Equal(2, (await vhost.Broker.GetQueueAsync("healthy"))?.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_Deliver_When_QueueRejectsMessageUntilItHasRoom(bool targetsQueue)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreateBoundedBusAsync(vhost, TestTimeouts.Signal, targetsQueue);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });

        // act
        // the bounded queue holds one message and rejects every further one with a basic.nack
        var send = SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });
        await Assert.ThrowsAsync<TimeoutException>(
            () => send.WaitAsync(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken));
        var whileFull = await ReadOrderIdsAsync(vhost, "bounded");
        await send;
        var afterRoom = await ReadOrderIdsAsync(vhost, "bounded");

        // assert
        Assert.Equal(["ORD-1"], whileFull);
        Assert.Equal(["ORD-2"], afterRoom);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SendAsync_Should_ThrowTransportUnavailable_When_QueueRejectsMessageLongerThanPublishTimeout(
        bool targetsQueue)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await CreateBoundedBusAsync(vhost, TimeSpan.FromSeconds(2), targetsQueue);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });

        // act
        var exception = await Assert.ThrowsAsync<TransportUnavailableException>(
            () => SendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 }));

        // assert
        Assert.Equal(
            "The broker rejected the message until the publish timeout of 00:00:02 elapsed. "
            + "This can happen when a queue is at its length limit.",
            exception.Message);
        Assert.False(Assert.IsType<PublishException>(exception.InnerException).IsReturn);
        Assert.Equal(["ORD-1"], await ReadOrderIdsAsync(vhost, "bounded"));
    }

    [Fact]
    public async Task PublishAsync_Should_FailWithoutPublishingAgain_When_QueueOfTheExchangeRejectsMessage()
    {
        // arrange
        // the exchange routes to a bounded queue and to an audit queue without a limit
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("orders");
                t.DeclareQueue("bounded")
                    .WithArgument("x-max-length", 1)
                    .WithArgument("x-overflow", "reject-publish");
                t.DeclareQueue("audit");
                t.DeclareBinding("orders", "bounded");
                t.DeclareBinding("orders", "audit");
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });

        // act
        // the bounded queue is full and rejects the message, the audit queue accepts it
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }));

        // assert
        Assert.Equal(
            "The broker rejected the message that was published to exchange 'orders'. "
            + "The queues that accepted it have received it, so it is not published again. "
            + "This can happen when a queue that is bound to the exchange is at its length limit.",
            exception.Message);
        Assert.False(Assert.IsType<PublishException>(exception.InnerException).IsReturn);
        Assert.Equal(["ORD-1", "ORD-2"], await ReadOrderIdsAsync(vhost, "audit"));
    }

    [Fact]
    public async Task SendAsync_Should_Deliver_When_PreviousPublishMadeBrokerCloseChannel()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var payments = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(payments)
            .AddMessageBus()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
                t.Endpoint("payments").Handler<PaymentHandler>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        Assert.True(await payments.WaitAsync(TestTimeouts.Signal));
        await DeleteExchangeAsync(vhost, "orders");

        // act
        var exception = await Assert.ThrowsAsync<AlreadyClosedException>(
            () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }));
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-3", Amount = 1 });

        // assert
        Assert.Equal<ushort?>(Constants.NotFound, exception.ShutdownReason?.ReplyCode);
        Assert.True(await payments.WaitAsync(TestTimeouts.Signal));
        Assert.Equal(["ORD-1", "ORD-3"], payments.Messages.Cast<ProcessPayment>().Select(p => p.OrderId).Order());
    }

    [Fact]
    public async Task PublishAsync_Should_DeclareExchangeAndBindingsAgain_When_PreviousPublishFoundExchangeDeleted()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareExchange("orders");
                t.DeclareQueue("audit");
                t.DeclareBinding("orders", "audit");
                t.DispatchEndpoint("orders-publish").ToExchange("orders").Publish<OrderCreated>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var dispatcher = bus.Transport.Dispatcher;
        var original = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);
        await DeleteExchangeAsync(vhost, "orders");

        // act
        // the broker deletes the bindings of an exchange together with it
        var exception = await Assert.ThrowsAsync<AlreadyClosedException>(
            () => PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }));
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });
        var current = await dispatcher.Connection.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal<ushort?>(Constants.NotFound, exception.ShutdownReason?.ReplyCode);
        Assert.Same(original, current);
        Assert.Equal(["ORD-1", "ORD-3"], await ReadOrderIdsAsync(vhost, "audit"));
    }

    [Fact]
    public async Task PublishAsync_Should_FailFast_When_MessageLargerThanMaxMessageSize()
    {
        // arrange
        await using var maxMessageSize = await fixture.Faults.SetMaxMessageSizeAsync(4096);
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);

        // act
        var exception = await Assert.ThrowsAsync<AlreadyClosedException>(
            () => PublishAsync(bus, new OrderCreated { OrderId = new string('x', 100_000) })
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });

        // assert
        Assert.Equal<ushort?>(Constants.PreconditionFailed, exception.ShutdownReason?.ReplyCode);
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));
        Assert.Equal("ORD-2", Assert.Single(orders.Messages.Cast<OrderCreated>()).OrderId);
    }

    [Fact]
    public async Task Fault_Should_ReachErrorQueue_When_ErrorQueueDeletedByOperator()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddEventHandler<FailingOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<FailingOrderHandler>())
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await Condition.WaitAsync(
            async () => (await vhost.Broker.GetQueueAsync("orders_error"))?.Messages == 1,
            TestTimeouts.Signal));

        // act
        // without mandatory, the broker confirms a message it cannot route and drops it
        await vhost.Faults.DeleteQueueAsync("orders_error");
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var deadLettered = await Condition.WaitAsync(
            async () => (await vhost.Broker.GetQueueAsync("orders_error"))?.Messages == 1,
            TestTimeouts.Signal);
        var pending = (await vhost.Broker.GetQueueAsync("orders"))?.Messages;

        // assert
        Assert.True(deadLettered);
        Assert.Equal(0, pending);
    }

    [Fact]
    public async Task Handler_Should_FaultMessageAndKeepConsuming_When_PublishInHandlerFails()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<InvoicingOrderHandler>()
            .AddRabbitMQ(t =>
            {
                t.DispatchEndpoint("invoices-publish").ToExchange("invoices").Publish<InvoiceRequested>();
                t.Endpoint("orders").Handler<InvoicingOrderHandler>();
            })
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(TestTimeouts.Signal));
        await DeleteExchangeAsync(vhost, "invoices");

        // act
        // the handler's publish to the deleted exchange makes the broker close its channel with 404
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var faulted = await Condition.WaitAsync(
            async () => (await vhost.Broker.GetQueueAsync("orders_error"))?.Messages == 1,
            TestTimeouts.Signal);
        await PublishAsync(bus, new OrderCreated { OrderId = InvoicingOrderHandler.WithoutInvoice });
        var nextHandled = await orders.WaitAsync(TestTimeouts.Signal);
        var queue = await vhost.Broker.GetQueueAsync("orders");

        // assert
        Assert.True(faulted);
        Assert.True(nextHandled);
        Assert.Equal(1, queue?.Consumers);
    }

    private static Task<TestBus> CreateOrdersBusAsync(VhostContext vhost, MessageRecorder orders)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();

    /// <summary>
    /// Creates a bus whose payments go to a queue that holds one message and rejects every further one: through
    /// a dispatch endpoint for the queue, or through the exchange that the naming convention gives the command.
    /// </summary>
    private static Task<TestBus> CreateBoundedBusAsync(VhostContext vhost, TimeSpan publishTimeout, bool targetsQueue)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.PublishTimeout(publishTimeout);
                t.DeclareQueue("bounded")
                    .WithArgument("x-max-length", 1)
                    .WithArgument("x-overflow", "reject-publish");
                if (targetsQueue)
                {
                    t.DispatchEndpoint("bounded-send").ToQueue("bounded").Send<ProcessPayment>();
                }
                else
                {
                    t.DeclareExchange("process-payment");
                    t.DeclareBinding("process-payment", "bounded");
                }
            })
            .BuildTestBusAsync();

    /// <summary>
    /// Takes every message out of the queue and returns the order ids in the order of the queue.
    /// </summary>
    private static async Task<List<string?>> ReadOrderIdsAsync(VhostContext vhost, string queue)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var orderIds = new List<string?>();
        while (await channel.BasicGetAsync(queue, autoAck: true, TestContext.Current.CancellationToken) is { } message)
        {
            // every message of these tests has an order id
            orderIds.Add(JsonSerializer.Deserialize<OrderCreated>(message.Body.Span, options)?.OrderId);
        }

        return orderIds;
    }

    private static async Task DeleteExchangeAsync(VhostContext vhost, string exchange)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.ExchangeDeleteAsync(exchange, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task SendAsync(TestBus bus, ProcessPayment message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
    }

    private static async Task PublishAsync(TestBus bus, OrderCreated message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? TestTimeouts.Signal);
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message, cts.Token);
    }

    public sealed class InvoiceRequested
    {
        public required string OrderId { get; init; }
    }

    public sealed class FailingOrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
            => throw new InvalidOperationException($"Order {message.OrderId} failed.");
    }

    public sealed class InvoicingOrderHandler(IMessageBus bus, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public const string WithoutInvoice = "ORD-WITHOUT-INVOICE";

        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (message.OrderId != WithoutInvoice)
            {
                await bus.PublishAsync(new InvoiceRequested { OrderId = message.OrderId }, cancellationToken);
            }

            recorder.Record(message);
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
