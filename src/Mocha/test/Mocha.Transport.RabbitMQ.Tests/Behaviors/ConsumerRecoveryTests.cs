using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection(RabbitMQCollections.ConsumerRecovery)]
public sealed class ConsumerRecoveryTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan s_consumerTimeout = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task Consumer_Should_RedeclareQueueAndBindings_When_QueueDeletedByOperator()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var recorder = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        Assert.True(await recorder.WaitAsync(s_timeout));

        // act
        await vhost.DeleteQueueAsync("orders");
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
            s_timeout);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var received = await recorder.WaitAsync(s_timeout);

        // assert
        Assert.True(reattached);
        Assert.True(received);
    }

    [Fact]
    public async Task Consumers_Should_KeepOtherEndpointsReceiving_When_ConsumerTimeoutExpiresForStuckHandler()
    {
        // arrange
        // RabbitMQ 4.3 evaluates consumer timeouts only for quorum queues
        await using var vhost = await fixture.CreateVhostAsync();
        await using var consumerTimeout = await fixture.SetConsumerTimeoutAsync(s_consumerTimeout);
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        var payments = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddKeyedSingleton("orders", orders)
            .AddKeyedSingleton("payments", payments)
            .AddMessageBus()
            .AddEventHandler<StuckOrderHandler>()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ(t =>
            {
                t.Queue("orders").Quorum().Handler<StuckOrderHandler>().MaxConcurrency(1);
                t.Endpoint("payments").Handler<PaymentHandler>();
            })
            .BuildTestBusAsync();

        try
        {
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // act
            // the broker closes the channel (before 4.3) or cancels the consumer (4.3) of the stuck handler
            await gate.Cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
            var paymentReceived = await payments.WaitAsync(s_timeout);
            await gate.Cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            var whileBlocked = (gate.Invocations, orders.Messages.Count, GetEndpoint(bus, "orders").IsHealthy);
            gate.Release.TrySetResult();
            var orderRedelivered = await orders.WaitAsync(s_timeout);

            // assert
            Assert.True(paymentReceived);
            Assert.Equal(["ORD-1"], payments.Messages.Cast<ProcessPayment>().Select(p => p.OrderId));
            Assert.Equal((1, 0, false), whileBlocked);
            Assert.True(orderRedelivered);
            Assert.Equal(["ORD-1"], orders.Messages.Cast<OrderCreated>().Select(o => o.OrderId));
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Message_Should_BeHandledAgainAfterFirstHandlerIsCancelled_When_ConsumerTimeoutExpires()
    {
        // arrange
        // RabbitMQ 4.3 evaluates consumer timeouts only for quorum queues
        await using var vhost = await fixture.CreateVhostAsync();
        await using var consumerTimeout = await fixture.SetConsumerTimeoutAsync(s_consumerTimeout);
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddKeyedSingleton("orders", orders)
            .AddMessageBus()
            .AddEventHandler<CooperativeOrderHandler>()
            .AddRabbitMQ(t => t.Queue("orders").Quorum().Handler<CooperativeOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();

        // act
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var handled = await orders.WaitAsync(s_timeout);
        var settled = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders") == 0,
            s_timeout);

        // assert
        Assert.True(handled);
        Assert.True(settled);
        Assert.Equal(2, gate.Invocations);
        Assert.Single(orders.Messages);
        Assert.True(gate.FirstCancelled);
    }

    [Fact]
    public async Task Consumer_Should_Reattach_When_ConnectionClosedWhileHandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        vhost.ConnectionSettings.NetworkRecoveryInterval = TimeSpan.FromSeconds(1);
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddKeyedSingleton("orders", orders)
            .AddMessageBus()
            .AddEventHandler<StuckOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>().MaxConcurrency(1))
            .BuildTestBusAsync();

        try
        {
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // act
            // the message is requeued when the connection closes, so a re-attached consumer receives it again
            await fixture.CloseAllConnectionsAsync();
            await gate.Cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            var whileBlocked = (
                Invocations: gate.Invocations,
                Handled: orders.Messages.Count,
                IsHealthy: GetEndpoint(bus, "orders").IsHealthy);
            gate.Release.TrySetResult();
            var redelivered = await orders.WaitAsync(s_timeout);
            var settled = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                s_timeout);
            var consumers = await vhost.GetConsumerCountsAsync();

            // assert
            Assert.Equal((1, 0, false), whileBlocked);
            Assert.True(redelivered);
            Assert.True(settled);
            Assert.Equal(1, consumers.GetValueOrDefault("orders"));
            Assert.Equal((1, true), (orders.Messages.Count, GetEndpoint(bus, "orders").IsHealthy));
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Message_Should_RunHandlerAgain_When_HandlerCompletedAfterItsChannelClosed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddKeyedSingleton("orders", orders)
            .AddMessageBus()
            .AddEventHandler<GatedOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<GatedOrderHandler>())
            .BuildTestBusAsync();

        try
        {
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // act
            // the consumer cannot reconnect while access is revoked, so the handler completes before the
            // broker redelivers its message
            await using (await vhost.RevokeAccessAsync())
            {
                await vhost.CloseConsumerConnectionsAsync();
                await gate.Cancelled.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
                gate.Release.TrySetResult();
                Assert.True(await orders.WaitAsync(s_timeout));
            }

            var settled = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                TimeSpan.FromSeconds(20));

            // assert
            Assert.True(settled);
            Assert.Equal(
                (2, 2, 0),
                (gate.Invocations, orders.Messages.Count, await vhost.GetMessageCountAsync("orders_error")));
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Consumer_Should_Attach_When_QueueReturnsAfterReconnect()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await DeclareQueueAsync(vhost, "orders");
        var orders = new MessageRecorder();
        var payments = new MessageRecorder();
        await using var bus = await CreateBusWithoutQueueProvisioningAsync(vhost, orders, payments);

        // act
        // the reconnect cannot attach while the queue is missing
        await using (await vhost.RevokeAccessAsync())
        {
            await fixture.CloseAllConnectionsAsync();
            await vhost.DeleteQueueAsync("orders");
        }

        await vhost.WaitForConsumerAsync("payments", s_timeout);
        await DeclareQueueAsync(vhost, "orders");
        var attached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
            TimeSpan.FromSeconds(8));

        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-recovered" });

        // assert
        Assert.True(attached, "The consumer waited for the periodic health check instead of retrying.");
        Assert.True(await orders.WaitAsync(s_timeout));
        Assert.Equal("ORD-recovered", Assert.Single(orders.Messages.Cast<OrderCreated>()).OrderId);
    }

    [Fact]
    public async Task Consumers_Should_KeepOtherQueuesFlowing_When_QueueDeletedWithoutAutoProvision()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await DeclareQueueAsync(vhost, "orders");
        var orders = new MessageRecorder();
        var payments = new MessageRecorder();
        await using var bus = await CreateBusWithoutQueueProvisioningAsync(vhost, orders, payments);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-0", Amount = 1 });

        // act
        // every re-attach attempt fails with 404, which closes only the channel of the attempt
        await vhost.DeleteQueueAsync("orders");
        await vhost.WaitForNoConsumerAsync("orders", s_timeout);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
        var paymentsReceived = await payments.WaitAsync(s_timeout, expectedCount: 2);

        await DeclareQueueAsync(vhost, "orders");
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
            TimeSpan.FromSeconds(20));
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
        var orderReceived = await orders.WaitAsync(s_timeout);

        // assert
        Assert.True(paymentsReceived);
        Assert.Equal(["ORD-0", "ORD-1"], payments.Messages.Cast<ProcessPayment>().Select(p => p.OrderId).Order());
        Assert.True(reattached);
        Assert.True(orderReceived);
        Assert.Equal(["ORD-2"], orders.Messages.Cast<OrderCreated>().Select(o => o.OrderId));
    }

    [Fact]
    public async Task Consumer_Should_KeepPrefetch_When_Reattached()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("orders", orders)
            .AddMessageBus()
            .AddEventHandler<RecordingOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<RecordingOrderHandler>().MaxPrefetch(7))
            .BuildTestBusAsync();
        var before = (await vhost.GetConsumerPrefetchAsync()).GetValueOrDefault("orders");

        // act
        await vhost.CloseConsumerConnectionsAsync();
        var reattached = await Condition.WaitAsync(
            async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
            s_timeout);
        var after = (await vhost.GetConsumerPrefetchAsync()).GetValueOrDefault("orders");

        // assert
        Assert.Equal(7, before);
        Assert.True(reattached);
        Assert.Equal(7, after);
    }

    [Fact]
    public async Task Consumer_Should_KeepConsuming_When_RawMessageHasNoEnvelope()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var orders = new MessageRecorder();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("orders", orders)
            .AddMessageBus()
            .AddEventHandler<RecordingOrderHandler>()
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<RecordingOrderHandler>())
            .BuildTestBusAsync();

        // act
        // a message from a client that does not set any of the properties or headers the bus writes
        await using (var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken))
        await using (var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken))
        {
            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "orders",
                body: "not an envelope"u8.ToArray(),
                cancellationToken: TestContext.Current.CancellationToken);
        }

        var drained = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders") == 0,
            s_timeout);
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var nextHandled = await orders.WaitAsync(s_timeout);

        // assert
        Assert.True(drained);
        Assert.Equal(1, await vhost.GetMessageCountAsync("orders_skipped"));
        Assert.Equal(0, await vhost.GetMessageCountAsync("orders_error"));
        Assert.True(nextHandled);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(32)]
    public async Task BatchHandler_Should_HandleEveryMessage_When_ConnectionClosesWhileBatchFills(int busConcurrency)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var batches = new BatchRecorder();
        using var buffered = new SemaphoreSlim(0);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(batches)
            .AddMessageBus()
            .AddConcurrencyLimiter(options => options.MaxConcurrency = busConcurrency)
            .AddBatchHandler<OrderBatchHandler>(options =>
            {
                options.MaxBatchSize = 10;
                options.BatchTimeout = TimeSpan.FromSeconds(30);
            })
            .AddRabbitMQ(t => t.Queue("batch-orders")
                .Handler<OrderBatchHandler>()
                .MaxConcurrency(10)
                .AddConcurrencyLimiter(options => options.MaxConcurrency = 10)
                .UseReceive(new ReceiveMiddlewareConfiguration(
                    (_, next) => context =>
                    {
                        // A partial batch is buffered before the consumer returns its pending task.
                        var pending = next(context);
                        buffered.Release();
                        return pending;
                    },
                    "BatchBuffered")))
            .BuildTestBusAsync();
        for (var i = 1; i <= 6; i++)
        {
            await PublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" });
        }

        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => buffered.WaitAsync(TestContext.Current.CancellationToken)))
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        await vhost.CloseConsumerConnectionsAsync();
        for (var i = 7; i <= 10; i++)
        {
            await PublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" });
        }

        var allHandled = await Condition.WaitAsync(() => batches.OrderIds.Count == 10, s_timeout);
        var drained = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("batch-orders") == 0, s_timeout);

        // assert
        Assert.True(allHandled);
        Assert.Equal(Enumerable.Range(1, 10).Select(i => $"ORD-{i}").Order(), batches.OrderIds.Order());
        Assert.Equal(1, batches.Count);
        Assert.True(drained);
    }

    private static async Task<TestBus> CreateBusWithoutQueueProvisioningAsync(
        VhostContext vhost,
        MessageRecorder orders,
        MessageRecorder payments)
        => await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("orders", orders)
            .AddKeyedSingleton("payments", payments)
            .AddMessageBus()
            .AddEventHandler<RecordingOrderHandler>()
            .AddRequestHandler<PaymentHandler>()
            .AddRabbitMQ(t =>
            {
                // an operator owns the queue, so the bus neither declares it nor declares it again
                t.DeclareQueue("orders").AutoProvision(false);
                t.Endpoint("orders").Handler<RecordingOrderHandler>();
                t.Endpoint("payments").Handler<PaymentHandler>();
            })
            .BuildTestBusAsync();

    private static async Task DeclareQueueAsync(VhostContext vhost, string queueName)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(
            TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
    }

    private static RabbitMQReceiveEndpoint GetEndpoint(TestBus bus, string queueName)
        => ((MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>())
            .Transports.OfType<RabbitMQMessagingTransport>().Single()
            .ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>().Single(endpoint => endpoint.Queue.Name == queueName);

    private static async Task PublishAsync(TestBus bus, OrderCreated message)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .PublishAsync(message, TestContext.Current.CancellationToken);
    }

    private static async Task SendAsync(TestBus bus, ProcessPayment message)
    {
        using var scope = bus.Provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>()
            .SendAsync(message, TestContext.Current.CancellationToken);
    }

    public sealed class HandlerGate
    {
        private int _invocations;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Invocations => Volatile.Read(ref _invocations);

        public bool FirstCancelled { get; set; }

        public bool IsFirstInvocation() => Interlocked.Increment(ref _invocations) == 1;
    }

    public sealed class StuckOrderHandler(HandlerGate gate, [FromKeyedServices("orders")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (gate.IsFirstInvocation())
            {
                gate.Started.TrySetResult();
                await using var registration = cancellationToken.Register(() => gate.Cancelled.TrySetResult());

                // ignores its cancellation token, as a blocking call without a token would
                await gate.Release.Task;
                return;
            }

            recorder.Record(message);
        }
    }

    public sealed class CooperativeOrderHandler(
        HandlerGate gate,
        [FromKeyedServices("orders")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (gate.IsFirstInvocation())
            {
                gate.Started.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    gate.FirstCancelled = true;
                    throw;
                }
            }

            recorder.Record(message);
        }
    }

    public sealed class GatedOrderHandler(HandlerGate gate, [FromKeyedServices("orders")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            if (gate.IsFirstInvocation())
            {
                gate.Started.TrySetResult();
                await using var registration = cancellationToken.Register(() => gate.Cancelled.TrySetResult());
                await gate.Release.Task;
            }

            recorder.Record(message);
        }
    }

    public sealed class RecordingOrderHandler([FromKeyedServices("orders")] MessageRecorder recorder)
        : IEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class BatchRecorder
    {
        private readonly ConcurrentDictionary<string, byte> _orderIds = new();
        private int _count;

        public ICollection<string> OrderIds => _orderIds.Keys;

        public int Count => Volatile.Read(ref _count);

        public void Record(IEnumerable<OrderCreated> orders)
        {
            Interlocked.Increment(ref _count);

            foreach (var order in orders)
            {
                _orderIds.TryAdd(order.OrderId, 0);
            }
        }
    }

    public sealed class OrderBatchHandler(BatchRecorder recorder) : IBatchEventHandler<OrderCreated>
    {
        public ValueTask HandleAsync(IMessageBatch<OrderCreated> batch, CancellationToken cancellationToken)
        {
            recorder.Record(batch);
            return default;
        }
    }

    public sealed class PaymentHandler([FromKeyedServices("payments")] MessageRecorder recorder)
        : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}
