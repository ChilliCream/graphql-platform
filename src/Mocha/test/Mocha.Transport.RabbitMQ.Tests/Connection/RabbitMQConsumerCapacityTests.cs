using System.Collections.Concurrent;
using System.Text;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection(RabbitMQCollections.Messaging)]
public sealed class RabbitMQConsumerCapacityTests(RabbitMQFixture fixture)
{
    private const string BlockedReason =
        "Queue 'orders': The consumer waits for a handler of a lost channel to return.";

    [Fact]
    public async Task Consumer_Should_ResumeReceiving_When_BlockedHandlerExits()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            "orders",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            gate.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        try
        {
            // act
            await registration.Channel!.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var blocked = await Condition.WaitAsync(
                () => gate.CancelledHandlers == 1 && manager.HealthFailureReason == BlockedReason,
                TestTimeouts.Signal);
            gate.Release.TrySetResult();
            var recovered = await Condition.WaitAsync(
                () => gate.Delivered.Count == 1 && manager.HealthFailureReason is null,
                TestTimeouts.Signal);

            // assert
            Assert.True(blocked);
            Assert.True(recovered);
            Assert.Equal(1, gate.PeakConcurrency);
            Assert.Equal(["order"], gate.Delivered);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Consumer_Should_UseRemainingCapacity_When_OneLostHandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            "orders",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            gate.HandleAsync,
            maxPrefetch: 2,
            maxConcurrency: 2,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order-0"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order-1"u8.ToArray(), TestContext.Current.CancellationToken);
        Assert.True(await Condition.WaitAsync(() => gate.Delivered.Count == 1 && gate.ActiveHandlers == 1, TestTimeouts.Signal));

        try
        {
            // act
            var lostChannel = registration.Channel!;
            await lostChannel.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var redelivered = await Condition.WaitAsync(
                () => gate.Delivered.Count == 2 && gate.CancelledHandlers == 1 && gate.ActiveHandlers == 1,
                TestTimeouts.Signal);

            // assert
            Assert.True(redelivered);
            Assert.Equal(
                new HandlerState(ActiveHandlers: 1, PeakConcurrency: 2, Invocations: 3),
                gate.State);
            Assert.Null(manager.HealthFailureReason);
            Assert.Equal(["order-0", "order-1"], gate.Delivered.Order());
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Consumer_Should_WaitForCapacity_When_AllLostHandlersIgnoreCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false, cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 2);
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            gate.HandleAsync,
            maxPrefetch: 4,
            maxConcurrency: 2,
            TestContext.Current.CancellationToken);
        for (var i = 0; i < 4; i++)
        {
            await channel.BasicPublishAsync("", "orders", Encoding.UTF8.GetBytes($"order-{i}"), TestContext.Current.CancellationToken);
        }

        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        try
        {
            // act
            await registration.Channel!.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var cancelled = await Condition.WaitAsync(() => gate.CancelledHandlers == 2, TestTimeouts.Signal);

            // the consumer must not consume, and so not prefetch, while no handler can take a message
            await Wait.UntilAsync(() => manager.HealthFailureReason == BlockedReason);
            await vhost.Broker.WaitForNoConsumerAsync("orders");
            var whileBlocked = (registration.IsAttached, gate.Invocations, gate.ActiveHandlers);
            gate.Release.TrySetResult();
            var delivered = await Condition.WaitAsync(
                () => gate.Delivered.Count == 4 && manager.HealthFailureReason is null,
                TestTimeouts.Signal);

            // assert
            Assert.Equal((false, 2, 2), whileBlocked);
            Assert.True(cancelled);
            Assert.True(delivered);
            Assert.Equal(2, gate.PeakConcurrency);
            Assert.Equal(["order-0", "order-1", "order-2", "order-3"], gate.Delivered.Order());
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task StopAsync_Should_CompleteWithinDeadline_When_LostChannelHandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false, cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            gate.HandleAsync,
            maxPrefetch: 1,
            maxConcurrency: 1,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        try
        {
            // act
            var lostChannel = registration.Channel!;
            await lostChannel.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var cancelled = await Condition.WaitAsync(() => gate.CancelledHandlers == 1, TestTimeouts.Signal);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await registration.StopAsync(deadline.Token).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            var activeAtStop = gate.ActiveHandlers;
            gate.Release.TrySetResult();
            var finished = await Condition.WaitAsync(() => gate.ActiveHandlers == 0, TestTimeouts.Signal);

            // assert
            Assert.True(cancelled);
            Assert.Equal(1, activeAtStop);
            Assert.True(finished);
            Assert.Equal(1, gate.Invocations);
            Assert.False(registration.IsAttached);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task DisposeAsync_Should_EndGracefulStop_When_HandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(
            "orders",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        await using var manager = TestManagers.CreateConsumerManager(
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.StartAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            gate.HandleAsync,
            1,
            1,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        try
        {
            // act
            var gracefulStop = registration.StopAsync(CancellationToken.None);
            await vhost.Broker.WaitForNoConsumerAsync("orders");
            var draining = !gracefulStop.IsCompleted;
            await registration.DisposeAsync().AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            await gracefulStop.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
            await vhost.Broker.WaitForMessagesAsync("orders", 1);

            // assert
            Assert.True(draining);
            Assert.True(await Condition.WaitAsync(() => gate.CancelledHandlers == 1, TestTimeouts.Signal));
            Assert.False(registration.IsAttached);
            Assert.Equal(1, gate.ActiveHandlers);
            Assert.Equal(1, (await vhost.Broker.GetQueueAsync("orders"))?.Messages);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    private sealed record HandlerState(int ActiveHandlers, int PeakConcurrency, int Invocations);

    private sealed class HandlerGate(int blockedHandlers)
    {
        private int _invocations;
        private int _activeHandlers;
        private int _peakConcurrency;
        private int _cancelledHandlers;

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentQueue<string> Delivered { get; } = new();
        public int Invocations => Volatile.Read(ref _invocations);
        public int ActiveHandlers => Volatile.Read(ref _activeHandlers);
        public int PeakConcurrency => Volatile.Read(ref _peakConcurrency);
        public int CancelledHandlers => Volatile.Read(ref _cancelledHandlers);
        public HandlerState State => new(ActiveHandlers, PeakConcurrency, Invocations);

        public async ValueTask HandleAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
        {
            var invocation = Interlocked.Increment(ref _invocations);
            var active = Interlocked.Increment(ref _activeHandlers);
            var peak = Volatile.Read(ref _peakConcurrency);
            while (active > peak)
            {
                var previous = Interlocked.CompareExchange(ref _peakConcurrency, active, peak);
                if (previous == peak)
                {
                    break;
                }

                peak = previous;
            }

            try
            {
                if (invocation <= blockedHandlers)
                {
                    await using var registration = cancellationToken.Register(() => Interlocked.Increment(ref _cancelledHandlers));
                    if (invocation == blockedHandlers)
                    {
                        Started.TrySetResult();
                    }

                    await Release.Task;
                }
                else
                {
                    Delivered.Enqueue(Encoding.UTF8.GetString(delivery.Body.Span));
                    await channel.BasicAckAsync(delivery.DeliveryTag, false, CancellationToken.None);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeHandlers);
            }
        }
    }
}
