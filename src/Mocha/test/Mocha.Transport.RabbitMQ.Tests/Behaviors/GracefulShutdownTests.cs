using System.Collections.Concurrent;
using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Behaviors;

[Collection("RabbitMQ")]
public sealed class GracefulShutdownTests(RabbitMQFixture fixture)
{
    private const string QueueName = "shutdown-orders";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task StopAsync_Should_CancelHandler_When_StopIsAlreadyCancelled()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate(ignoreCancellation: false);
        await using var bus = await StartGatedBusAsync(vhost, gate);
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(new CancellationToken(canceled: true)).AsTask();
        try
        {
            await stop.WaitAsync(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken);
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        // assert
        new
        {
            HandlerCancelled = gate.Token.IsCancellationRequested,
            RuntimeStopped = !runtime.IsStarted,
            ReadyMessages = await GetReadyMessagesAsync(vhost)
        }.MatchInlineSnapshot(
            """
            {
              "HandlerCancelled": true,
              "RuntimeStopped": true,
              "ReadyMessages": 1
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_RequeueWithoutFault_When_HandlerFailsAfterCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate(ignoreCancellation: false);
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<FailingAfterCancellationHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<FailingAfterCancellationHandler>())
            .BuildTestBusAsync();
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        await runtime.StopAsync(new CancellationToken(canceled: true))
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(8), TestContext.Current.CancellationToken);

        // assert
        new
        {
            HandlerCancelled = gate.Token.IsCancellationRequested,
            ReadyMessages = await GetReadyMessagesAsync(vhost),
            FaultedMessages = await GetReadyMessagesAsync(vhost, $"{QueueName}_error")
        }.MatchInlineSnapshot(
            """
            {
              "HandlerCancelled": true,
              "ReadyMessages": 1,
              "FaultedMessages": 0
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_AcknowledgeInFlightMessage_When_HandlerFinishesBeforeDeadline()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate(ignoreCancellation: false);
        await using var bus = await StartGatedBusAsync(vhost, gate);
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(200, TestContext.Current.CancellationToken);
        var completedBeforeRelease = stop.IsCompleted;
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            CompletedBeforeRelease = completedBeforeRelease,
            HandlerCompleted = gate.Completed.Task.IsCompleted,
            HandlerCancelled = gate.Token.IsCancellationRequested,
            ReadyMessages = await GetReadyMessagesAsync(vhost)
        }.MatchInlineSnapshot(
            """
            {
              "CompletedBeforeRelease": false,
              "HandlerCompleted": true,
              "HandlerCancelled": true,
              "ReadyMessages": 0
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_RequeueUnfinishedMessage_When_HandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate(ignoreCancellation: true);
        await using var bus = await StartGatedBusAsync(vhost, gate);
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await PublishAsync(bus, "ORD-2", "ORD-3");
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(new CancellationToken(canceled: true)).AsTask();
        var recorder = new MessageRecorder();
        bool redelivered;
        try
        {
            await stop.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await using var second = await new ServiceCollection()
                .AddSingleton(vhost.ConnectionFactory)
                .AddSingleton(recorder)
                .AddMessageBus()
                .AddEventHandler<OrderCreatedHandler>()
                .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderCreatedHandler>())
                .BuildTestBusAsync();
            redelivered = await recorder.WaitAsync(s_timeout, expectedCount: 3);
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        await gate.Completed.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            Redelivered = redelivered,
            RuntimeStopped = !runtime.IsStarted,
            Orders = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Order().ToArray()
        }.MatchInlineSnapshot(
            """
            {
              "Redelivered": true,
              "RuntimeStopped": true,
              "Orders": [
                "ORD-1",
                "ORD-2",
                "ORD-3"
              ]
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_RequeuePrefetchedMessages_When_OnlyInFlightHandlerFinishes()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gate = new HandlerGate(ignoreCancellation: false);
        await using var bus = await StartGatedBusAsync(vhost, gate);
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await PublishAsync(bus, "ORD-2", "ORD-3");
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(200, TestContext.Current.CancellationToken);
        gate.Release.TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var recorder = new MessageRecorder();
        await using var second = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderCreatedHandler>())
            .BuildTestBusAsync();
        var received = await recorder.WaitAsync(s_timeout, expectedCount: 2);

        // assert
        new
        {
            FirstHandlerCalls = gate.Calls,
            Redelivered = received,
            Orders = recorder.Messages.Cast<OrderCreated>().Select(m => m.OrderId).Order().ToArray()
        }.MatchInlineSnapshot(
            """
            {
              "FirstHandlerCalls": 1,
              "Redelivered": true,
              "Orders": [
                "ORD-2",
                "ORD-3"
              ]
            }
            """);
    }

    [Fact]
    public async Task StopAsync_Should_RequeuePrefetchedMessage_When_DispatchSlotFreesWhileStopping()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        var gates = new OrderGates();
        await using var bus = await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gates)
            .AddMessageBus()
            .AddEventHandler<OrderGatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderGatedHandler>().MaxConcurrency(2).MaxPrefetch(3))
            .BuildTestBusAsync();
        await PublishAsync(bus, "ORD-1", "ORD-2");
        await gates.Started("ORD-1").Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await gates.Started("ORD-2").Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // both dispatch slots are busy, so ORD-3 waits in the prefetch buffer of the consumer
        await PublishAsync(bus, "ORD-3");
        await WaitForQueueAsync(vhost, q => q.MessageCount == 0);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        var stop = runtime.StopAsync(TestContext.Current.CancellationToken).AsTask();
        await WaitForQueueAsync(vhost, q => q.ConsumerCount == 0);
        gates.Release("ORD-1").TrySetResult();
        var requeuedWhileStopping = await WaitForQueueAsync(vhost, q => q.MessageCount == 1);
        var stoppedBeforeRelease = stop.IsCompleted;
        gates.Release("ORD-2").TrySetResult();
        await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        new
        {
            RequeuedWhileStopping = requeuedWhileStopping,
            StoppedBeforeRelease = stoppedBeforeRelease,
            PrefetchedMessageHandled = gates.Started("ORD-3").Task.IsCompleted
        }.MatchInlineSnapshot(
            """
            {
              "RequeuedWhileStopping": true,
              "StoppedBeforeRelease": false,
              "PrefetchedMessageHandled": false
            }
            """);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_Should_CancelHandlerAndReturnMessage_When_ProviderStopsActiveBus(bool ignoreCancellation)
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync($"provider_shutdown_{ignoreCancellation}");
        var gate = new HandlerGate(ignoreCancellation);
        await using var bus = await StartGatedBusAsync(vhost, gate);
        await PublishAsync(bus, "ORD-1");
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var runtime = (MessagingRuntime)bus.Provider.GetRequiredService<IMessagingRuntime>();

        // act
        bool redelivered;
        try
        {
            await bus.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
            var recorder = new MessageRecorder();
            await using var second = await new ServiceCollection()
                .AddSingleton(vhost.ConnectionFactory)
                .AddSingleton(recorder)
                .AddMessageBus()
                .AddEventHandler<OrderCreatedHandler>()
                .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<OrderCreatedHandler>())
                .BuildTestBusAsync();
            redelivered = await recorder.WaitAsync(s_timeout);
        }
        finally
        {
            gate.Release.TrySetResult();
        }

        // assert
        new
        {
            HandlerCancelled = gate.Token.IsCancellationRequested,
            RuntimeStopped = !runtime.IsStarted,
            Redelivered = redelivered
        }.MatchInlineSnapshot(
            """
            {
              "HandlerCancelled": true,
              "RuntimeStopped": true,
              "Redelivered": true
            }
            """);
    }

    private static async Task<TestBus> StartGatedBusAsync(VhostContext vhost, HandlerGate gate)
        => await new ServiceCollection()
            .AddSingleton(vhost.ConnectionFactory)
            .AddSingleton(gate)
            .AddMessageBus()
            .AddEventHandler<GatedHandler>()
            .AddRabbitMQ(t => t.Endpoint(QueueName).Handler<GatedHandler>().MaxConcurrency(1).MaxPrefetch(3))
            .BuildTestBusAsync();

    private static async Task PublishAsync(TestBus bus, params string[] orderIds)
    {
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        foreach (var orderId in orderIds)
        {
            await messageBus.PublishAsync(new OrderCreated { OrderId = orderId }, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<uint> GetReadyMessagesAsync(VhostContext vhost, string queueName = QueueName)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var queue = await channel.QueueDeclarePassiveAsync(queueName, TestContext.Current.CancellationToken);
        return queue.MessageCount;
    }

    private static async Task<bool> WaitForQueueAsync(VhostContext vhost, Func<QueueDeclareOk, bool> condition)
    {
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (true)
        {
            var queue = await channel.QueueDeclarePassiveAsync(QueueName, TestContext.Current.CancellationToken);
            if (condition(queue))
            {
                return true;
            }

            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(50, TestContext.Current.CancellationToken);
        }
    }

    public sealed class OrderGates
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _started = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _released = new();

        public TaskCompletionSource Started(string orderId)
            => _started.GetOrAdd(orderId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

        public TaskCompletionSource Release(string orderId)
            => _released.GetOrAdd(orderId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
    }

    public sealed class OrderGatedHandler(OrderGates gates) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gates.Started(message.OrderId).TrySetResult();
            await gates.Release(message.OrderId).Task.WaitAsync(cancellationToken);
        }
    }

    public sealed class HandlerGate(bool ignoreCancellation)
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken Token { get; set; }

        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IgnoreCancellation => ignoreCancellation;

        public int Calls;
    }

    public sealed class FailingAfterCancellationHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Token = cancellationToken;
            gate.Started.TrySetResult();

            try
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw new InvalidOperationException("The order could not be saved.");
            }
        }
    }

    public sealed class GatedHandler(HandlerGate gate) : IEventHandler<OrderCreated>
    {
        public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
        {
            gate.Token = cancellationToken;
            Interlocked.Increment(ref gate.Calls);
            gate.Started.TrySetResult();
            if (gate.IgnoreCancellation)
            {
                await gate.Release.Task;
            }
            else
            {
                await gate.Release.Task.WaitAsync(cancellationToken);
            }

            gate.Completed.TrySetResult();
        }
    }
}
