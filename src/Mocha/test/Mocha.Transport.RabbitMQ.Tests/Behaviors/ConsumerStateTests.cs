using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

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
        new { Reattached = reattached, Received = received }.MatchInlineSnapshot(
            """
            {
              "Reattached": true,
              "Received": true
            }
            """);
    }

    [Fact]
    public async Task Consumers_Should_KeepReceiving_When_ConsumerTimeoutClosesChannelOfStuckHandler()
    {
        // arrange
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
                t.Endpoint("orders").Handler<StuckOrderHandler>();
                t.Endpoint("payments").Handler<PaymentHandler>();
            })
            .BuildTestBusAsync();

        try
        {
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // act
            // the broker closes the channel of the stuck handler once it holds the delivery past the timeout
            var channelClosed = await Condition.WaitAsync(() => logs.Contains("ConsumerChannelLost"), s_timeout);
            await SendAsync(bus, new ProcessPayment { OrderId = "ORD-1", Amount = 1 });
            var paymentReceived = await payments.WaitAsync(s_timeout);
            var orderRedelivered = await orders.WaitAsync(s_timeout);
            var stillStuck = !gate.Release.Task.IsCompleted;

            // assert
            new
            {
                ChannelClosed = channelClosed,
                PaymentReceived = paymentReceived,
                OrderRedelivered = orderRedelivered,
                StillStuck = stillStuck
            }.MatchInlineSnapshot(
                """
                {
                  "ChannelClosed": true,
                  "PaymentReceived": true,
                  "OrderRedelivered": true,
                  "StillStuck": true
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
    public async Task Message_Should_BeHandledAgain_When_ConsumerTimeoutCancelsHandler()
    {
        // arrange
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
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<CooperativeOrderHandler>())
            .BuildTestBusAsync();

        // act
        await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
        var handled = await orders.WaitAsync(s_timeout);
        var settled = await Condition.WaitAsync(
            async () => await vhost.GetMessageCountAsync("orders") == 0,
            s_timeout);

        // assert
        new
        {
            Handled = handled,
            Settled = settled,
            gate.Invocations,
            Completions = orders.Messages.Count
        }.MatchInlineSnapshot(
            """
            {
              "Handled": true,
              "Settled": true,
              "Invocations": 2,
              "Completions": 1
            }
            """);
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
            .AddRabbitMQ(t => t.Endpoint("orders").Handler<StuckOrderHandler>())
            .BuildTestBusAsync();

        try
        {
            await PublishAsync(bus, new OrderCreated { OrderId = "ORD-1" });
            await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

            // act
            // the message is requeued when the connection closes, so a re-attached consumer receives it again
            await fixture.CloseAllConnectionsAsync();
            var redeliveredWhileStuck = await orders.WaitAsync(s_timeout);

            // the abandoned handler finishes afterwards, and its delivery tag belongs to the lost channel
            gate.Release.TrySetResult();
            var settled = await Condition.WaitAsync(
                async () => await vhost.GetMessageCountAsync("orders") == 0,
                s_timeout);
            var consumers = await vhost.GetConsumerCountsAsync();

            // assert
            new
            {
                RedeliveredWhileStuck = redeliveredWhileStuck,
                Settled = settled,
                Consumers = consumers.GetValueOrDefault("orders"),
                Completions = orders.Messages.Count
            }.MatchInlineSnapshot(
                """
                {
                  "RedeliveredWhileStuck": true,
                  "Settled": true,
                  "Consumers": 1,
                  "Completions": 1
                }
                """);
        }
        finally
        {
            gate.Release.TrySetResult();
            logs.WriteTo(output);
        }
    }

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
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            recorder.Record(message);
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
