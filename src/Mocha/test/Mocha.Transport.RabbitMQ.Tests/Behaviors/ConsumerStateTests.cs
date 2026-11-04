using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class ConsumerStateTests(RabbitMQFixture fixture, ITestOutputHelper output)
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
        var logs = new LogRecorder();
        await using var bus = await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
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
            var consumerLost = await Condition.WaitAsync(
                () => logs.Contains("ConsumerChannelLost") || logs.Contains("ConsumerCancelledByBroker"),
                s_timeout);
            await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
            var paymentReceived = await payments.WaitAsync(s_timeout);
            var blocked = await Condition.WaitAsync(() => logs.Contains("ConsumerRecoveryBlocked"), s_timeout);
            var whileBlocked = (gate.Invocations, orders.Messages.Count, GetEndpoint(bus, "orders").IsHealthy);
            gate.Release.TrySetResult();
            var orderRedelivered = await orders.WaitAsync(s_timeout);

            // assert
            Assert.Equal((true, true, true), (consumerLost, paymentReceived, blocked));
            Assert.Equal((1, 0, false), whileBlocked);
            Assert.True(orderRedelivered);
        }
        finally
        {
            gate.Release.TrySetResult();
            logs.WriteTo(output);
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
        var logs = new LogRecorder();
        await using var bus = await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
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
            var blocked = await Condition.WaitAsync(() => logs.Contains("ConsumerRecoveryBlocked"), s_timeout);
            var whileBlocked = new
            {
                RecoveryBlocked = blocked,
                gate.Invocations,
                Handled = orders.Messages.Count,
                GetEndpoint(bus, "orders").IsHealthy
            };
            gate.Release.TrySetResult();
            var redelivered = await orders.WaitAsync(s_timeout);
            var settled = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                s_timeout);
            var consumers = await vhost.GetConsumerCountsAsync();

            // assert
            new
            {
                WhileBlocked = whileBlocked,
                Redelivered = redelivered,
                Settled = settled,
                Consumers = consumers.GetValueOrDefault("orders"),
                Handled = orders.Messages.Count,
                GetEndpoint(bus, "orders").IsHealthy
            }.MatchInlineSnapshot(
                """
                {
                  "WhileBlocked": {
                    "RecoveryBlocked": true,
                    "Invocations": 1,
                    "Handled": 0,
                    "IsHealthy": false
                  },
                  "Redelivered": true,
                  "Settled": true,
                  "Consumers": 1,
                  "Handled": 1,
                  "IsHealthy": true
                }
                """);
        }
        finally
        {
            gate.Release.TrySetResult();
            logs.WriteTo(output);
        }
    }

    [Fact]
    public async Task Message_Should_RunHandlerAgain_When_HandlerCompletedAfterItsChannelClosed()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate();
        var orders = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
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
                Assert.True(await Condition.WaitAsync(() => logs.Contains("ConsumerChannelLost"), s_timeout));
                gate.Release.TrySetResult();
                Assert.True(await orders.WaitAsync(s_timeout));
                await Task.Delay(TimeSpan.FromMilliseconds(500), TestContext.Current.CancellationToken);
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
            logs.WriteTo(output);
        }
    }

    [Fact]
    public async Task Consumer_Should_Attach_When_QueueReturnsAfterReconnect()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await DeclareQueueAsync(vhost, "orders");
        var orders = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusWithoutQueueProvisioningAsync(vhost, orders, logs);

        try
        {
            // act
            // the reconnect cannot attach while the queue is missing
            await using (await vhost.RevokeAccessAsync())
            {
                await fixture.CloseAllConnectionsAsync();
                await vhost.DeleteQueueAsync("orders");
            }

            Assert.True(await Condition.WaitAsync(() => logs.Contains("FailedToReconnectConsumerForQueue"), s_timeout));
            await DeclareQueueAsync(vhost, "orders");
            var attached = await Condition.WaitAsync(
                async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
                TimeSpan.FromSeconds(8));

            // assert
            Assert.True(attached, "The consumer waited for the periodic health check instead of retrying.");
        }
        finally
        {
            logs.WriteTo(output);
        }
    }

    [Fact]
    public async Task Consumers_Should_KeepOtherQueuesFlowing_When_QueueDeletedWithoutAutoProvision()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await DeclareQueueAsync(vhost, "orders");
        var orders = new MessageRecorder();
        var logs = new LogRecorder();
        await using var bus = await CreateBusWithoutQueueProvisioningAsync(vhost, orders, logs);
        await SendAsync(bus, new ProcessPayment { OrderId = "ORD-0", Amount = 1 });
        var before = await vhost.GetStateAsync();

        try
        {
            // act
            // every re-attach attempt fails with 404, which closes only the channel of the attempt
            await vhost.DeleteQueueAsync("orders");
            await Task.Delay(TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken);
            var whileMissing = await vhost.GetStateAsync();
            await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
            var paymentsReceived = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("payments") == 0,
                s_timeout);

            await DeclareQueueAsync(vhost, "orders");
            var reattached = await Condition.WaitAsync(
                async () => (await vhost.GetConsumerCountsAsync()).GetValueOrDefault("orders") == 1,
                TimeSpan.FromSeconds(20));
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-2" });
            var orderReceived = await orders.WaitAsync(s_timeout);

            // assert
            Assert.Equal(
                (2, 2, 3),
                (whileMissing.Connections, whileMissing.ConsumerChannels, before.ConsumerChannels));
            Assert.True(paymentsReceived);
            Assert.True(reattached);
            Assert.True(orderReceived);
        }
        finally
        {
            logs.WriteTo(output);
        }
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

    [Fact]
    public async Task BatchHandler_Should_HandleEveryMessage_When_ConsumerTimeoutElapsesWhileBatchFills()
    {
        // arrange
        // RabbitMQ 4.3 evaluates consumer timeouts only for quorum queues
        await using var consumerTimeout = await fixture.SetConsumerTimeoutAsync(s_consumerTimeout);
        await using var vhost = await fixture.CreateVhostAsync();
        var batches = new BatchRecorder();
        var logs = new LogRecorder();
        await using var bus = await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(batches)
            .AddMessageBus()
            .AddBatchHandler<OrderBatchHandler>(options =>
            {
                options.MaxBatchSize = 10;
                options.BatchTimeout = TimeSpan.FromSeconds(5);
            })
            .AddRabbitMQ(t => t.Queue("batch-orders").Quorum().Handler<OrderBatchHandler>().MaxConcurrency(10))
            .BuildTestBusAsync();

        try
        {
            // act
            // the batch waits longer for more messages than the broker lets a consumer hold a delivery
            for (var i = 1; i <= 6; i++)
            {
                await PublishAsync(bus, new OrderCreated { OrderId = $"ORD-{i}" });
            }

            var consumerLost = await Condition.WaitAsync(
                () => logs.Contains("ConsumerChannelLost") || logs.Contains("ConsumerCancelledByBroker"),
                s_timeout);
            await using var recoveryTimeout = await fixture.SetConsumerTimeoutAsync(TimeSpan.FromMinutes(30));
            await fixture.CloseAllConnectionsAsync();
            var allHandled = await Condition.WaitAsync(() => batches.OrderIds.Count == 6, s_timeout);
            var drained = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("batch-orders") == 0,
                s_timeout);
            var consumers = await vhost.GetConsumerCountsAsync();
            output.WriteLine($"batches: {batches.Count}");

            // assert
            Assert.True(consumerLost);
            Assert.True(allHandled);
            Assert.True(drained);
            Assert.Equal(1, consumers.GetValueOrDefault("batch-orders"));
        }
        finally
        {
            logs.WriteTo(output);
        }
    }

    private static async Task<TestBus> CreateBusWithoutQueueProvisioningAsync(
        VhostContext vhost,
        MessageRecorder orders,
        LogRecorder logs)
        => await new ServiceCollection()
            .AddLogging(builder => builder.AddProvider(logs))
            .AddSingleton(vhost.ConnectionFactory)
            .AddKeyedSingleton("orders", orders)
            .AddKeyedSingleton("payments", new MessageRecorder())
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
