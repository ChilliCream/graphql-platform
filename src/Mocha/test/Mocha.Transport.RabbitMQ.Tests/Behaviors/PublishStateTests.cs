using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConnectionRecovery)]
public sealed class PublishStateTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task SendAsync_Should_Throw_When_BrokerRejectsMessage()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("bounded")
                    .WithArgument("x-max-length", 1)
                    .WithArgument("x-overflow", "reject-publish");
                t.DispatchEndpoint("bounded-send").ToQueue("bounded").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

        // act
        var first = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var second = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-2", Amount = 1 });
        var depth = await vhost.GetMessageCountAsync("bounded");

        // assert
        Assert.Equal("sent", first);
        Assert.Equal("PublishException", second);
        Assert.Equal(1, depth);
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
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        Assert.True(await payments.WaitAsync(s_timeout));
        await DeleteExchangeAsync(vhost, "orders");

        // act
        // publishing to an exchange that no longer exists makes the broker close the channel with 404
        var publish = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var send = await TrySendAsync(bus, new ProcessPayment { OrderId = "ORD-3", Amount = 1 });
        var received = await payments.WaitAsync(s_timeout);

        // assert
        Assert.Equal("AlreadyClosedException", publish);
        Assert.Equal("sent", send);
        Assert.True(received);
    }

    [Fact]
    public async Task PublishAsync_Should_NotReportSuccess_When_ConnectionBlocked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        string whileBlocked;
        TimeSpan elapsed;
        await using (await fixture.BlockPublishersAsync())
        {
            var stopwatch = Stopwatch.StartNew();
            whileBlocked = await TryPublishAsync(
                bus,
                new OrderCreated { OrderId = "ORD-2" },
                TimeSpan.FromSeconds(2));
            elapsed = stopwatch.Elapsed;
        }

        var deliveredAfterUnblock = await orders.WaitAsync(s_timeout);

        // assert
        Assert.Equal("TaskCanceledException", whileBlocked);
        Assert.True(elapsed < TimeSpan.FromSeconds(5));
        Assert.True(deliveredAfterUnblock);
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_ConnectionClosedWhileAwaitingConfirm()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        // the broker reads nothing while the alarm is raised, so the publish waits for its confirm when the
        // connection closes
        Task<string> publish;
        await using (await fixture.BlockPublishersAsync())
        {
            publish = TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(30));
            Assert.True(await Condition.WaitAsync(() => logs.Contains("ConnectionBlocked"), s_timeout));
            await fixture.CloseAllConnectionsAsync();
            Assert.True(await Condition.WaitAsync(() => logs.Contains("PublishFailedRetrying"), s_timeout));
        }

        var outcome = await publish.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        var received = await orders.WaitAsync(s_timeout);

        // assert
        Assert.Equal("sent", outcome);
        Assert.True(received);
    }

    [Fact]
    public async Task PublishAsync_Should_ThrowWithinToken_When_BrokerStopped()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        string outcome;
        TimeSpan elapsed;
        await using (await fixture.StopBrokerAsync())
        {
            var stopwatch = Stopwatch.StartNew();
            outcome = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(3));
            elapsed = stopwatch.Elapsed;
        }

        // assert
        Assert.Equal("cancelled", Normalize(outcome));
        Assert.True(elapsed < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task PublishAsync_Should_CompleteEveryCall_When_ConnectionClosedDuringConcurrentPublishes()
    {
        // arrange
        const int count = 500;
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-0" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        var publishes = Enumerable.Range(1, count)
            .Select(i => TryPublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" }, TimeSpan.FromSeconds(30)))
            .ToList();
        await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        await fixture.CloseAllConnectionsAsync();
        var results = await Task.WhenAll(publishes)
            .WaitAsync(TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);
        var received = await orders.WaitAsync(TimeSpan.FromSeconds(30), expectedCount: count);
        var state = await vhost.GetStateAsync();

        // assert
        Assert.Equal(
            new Dictionary<string, int> { ["sent"] = 500 },
            results.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count()));
        Assert.True(received);
        Assert.Equal(2, state.Connections);
        Assert.True(state.IdleChannels <= 10);
    }

    [Fact]
    public async Task SendAsync_Should_StayWithinChannelLimit_When_ThousandsSendConcurrently()
    {
        // arrange
        const int count = 3000;
        await using var vhost = await fixture.CreateVhostAsync();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(t =>
            {
                t.DeclareQueue("burst");
                t.DispatchEndpoint("burst-send").ToQueue("burst").Send<ProcessPayment>();
            })
            .BuildTestBusAsync();

        // act
        // a publish keeps its channel until the broker confirms it, and a connection allows 2047 channels
        var sends = Enumerable.Range(0, count)
            .Select(i => TrySendAsync(bus, new ProcessPayment { OrderId = $"ORD-{i}", Amount = 1 }))
            .ToList();
        var peakChannels = 0;
        var all = Task.WhenAll(sends);
        while (!all.IsCompleted)
        {
            var state = await vhost.GetStateAsync();
            peakChannels = Math.Max(peakChannels, state.IdleChannels + state.ConsumerChannels);
            await Task.WhenAny(all, Task.Delay(200, TestContext.Current.CancellationToken));
        }

        var results = await all;
        var depth = await vhost.GetMessageCountAsync("burst");

        // assert
        Assert.Equal(
            new Dictionary<string, int> { ["sent"] = 3000 },
            results.GroupBy(r => r).ToDictionary(g => g.Key, g => g.Count()));
        Assert.Equal(3000, depth);
        Assert.True(peakChannels <= 110);
    }

    [Fact]
    public async Task PublishAsync_Should_Succeed_When_PreviousPublishTimedOut()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.ContinuationTimeout = TimeSpan.FromSeconds(1);
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));

        // act
        // the broker answers nothing while it is frozen, so the publish gives up when its token is cancelled
        string whileFrozen;
        await using (await vhost.FreezeConnectionsAsync())
        {
            whileFrozen = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" }, TimeSpan.FromSeconds(2));
        }

        var afterwards = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-3" });
        var received = await Condition.WaitAsync(
            () => orders.Messages.OfType<OrderCreated>().Any(o => o.OrderId == "ORD-3"),
            s_timeout);

        // assert
        Assert.Equal("cancelled", Normalize(whileFrozen));
        Assert.Equal("sent", afterwards);
        Assert.True(received);
    }

    [Fact]
    public async Task PublishAsync_Should_FailFast_When_MessageLargerThanMaxMessageSize()
    {
        // arrange
        await using var maxMessageSize = await fixture.SetMaxMessageSizeAsync(4096);
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await CreateOrdersBusAsync(vhost, orders);

        // act
        // the broker closes the channel with 406, and publishing the message again fails the same way
        var stopwatch = Stopwatch.StartNew();
        var oversized = await TryPublishAsync(bus, new OrderCreated { OrderId = new string('x', 100_000) });
        var elapsed = stopwatch.Elapsed;
        var next = await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var received = await orders.WaitAsync(s_timeout);

        // assert
        Assert.Equal("AlreadyClosedException", oversized);
        Assert.True(elapsed < TimeSpan.FromSeconds(5));
        Assert.Equal("sent", next);
        Assert.True(received);
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
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders_error") == 1,
            s_timeout));

        // act
        // without mandatory, the broker confirms a message it cannot route and drops it
        await vhost.DeleteQueueAsync("orders_error");
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var deadLettered = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders_error") == 1,
            s_timeout);
        var pending = await vhost.GetMessageCountAsync("orders");

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
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await orders.WaitAsync(s_timeout));
        await DeleteExchangeAsync(vhost, "invoices");

        // act
        // the handler's publish to the deleted exchange makes the broker close its channel with 404
        await TryPublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var faulted = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders_error") == 1,
            s_timeout);
        await TryPublishAsync(bus, new OrderCreated { OrderId = InvoicingOrderHandler.WithoutInvoice });
        var nextHandled = await orders.WaitAsync(s_timeout);
        var consumers = await vhost.GetConsumerCountsAsync();

        // assert
        Assert.True(faulted);
        Assert.True(nextHandled);
        Assert.Equal(1, consumers.GetValueOrDefault("orders"));
    }

    [Fact]
    public async Task PublishAsync_Should_Deliver_When_DispatchOnlyTransportRestarted()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var consumer = await CreateOrdersBusAsync(vhost, orders);
        await using var publisher = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddMessageBus()
            .AddRabbitMQ(_ => { })
            .BuildTestBusAsync();
        var runtime = (MessagingRuntime)publisher.Provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        await transport.StopAsync(runtime, TestContext.Current.CancellationToken);
        await transport.StartAsync(runtime, TestContext.Current.CancellationToken);
        var outcome = await TryPublishAsync(publisher, new OrderCreated { OrderId = "ORD-1" });
        var received = await orders.WaitAsync(s_timeout);

        // assert
        Assert.Equal("sent", outcome);
        Assert.True(received);
    }

    [Fact]
    public async Task Consumer_Should_KeepAcking_When_PublishingConnectionBlocked()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<GatedOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<GatedOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();
        for (var i = 1; i <= 5; i++)
        {
            await TryPublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" });
        }

        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        // the alarm blocks connections that publish, and the consumer connection only acks
        bool drained;
        await using (await fixture.BlockPublishersAsync())
        {
            gate.Release.TrySetResult();
            drained = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                s_timeout);
        }

        // assert
        Assert.True(drained);
        Assert.Equal(5, orders.Messages.Count);
    }

    private static Task<TestBus> CreateOrdersBusAsync(VhostContext vhost, MessageRecorder orders)
        => new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(orders)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();

    private static string Normalize(string outcome)
        => outcome is nameof(TaskCanceledException) or nameof(OperationCanceledException) ? "cancelled" : outcome;

    private static async Task DeleteExchangeAsync(VhostContext vhost, string exchange)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.ExchangeDeleteAsync(exchange, cancellationToken: TestContext.Current.CancellationToken);
    }

    private static async Task<string> TrySendAsync(TestBus bus, ProcessPayment message)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(s_timeout);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().SendAsync(message, cts.Token);
            return "sent";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
    }

    private static async Task<string> TryPublishAsync(TestBus bus, OrderCreated message, TimeSpan? timeout = null)
    {
        using var scope = bus.Provider.CreateScope();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(timeout ?? s_timeout);

        try
        {
            await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(message, cts.Token);
            return "sent";
        }
        catch (Exception ex)
        {
            return ex.GetType().Name;
        }
    }

    public sealed class InvoiceRequested
    {
        public required string OrderId { get; init; }
    }

    public sealed class HandlerGate
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class FailingOrderHandler : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
            => throw new InvalidOperationException($"Order {message.OrderId} failed.");
    }

    public sealed class GatedOrderHandler(HandlerGate gate, MessageRecorder recorder) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Started.TrySetResult();
            await gate.Release.Task.WaitAsync(cancellationToken);
            recorder.Record(message);
        }
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
