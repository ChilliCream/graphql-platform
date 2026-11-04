using System.Collections.Concurrent;
using System.Text;
using CookieCrumble;
using Microsoft.Extensions.Logging;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

[Collection("RabbitMQ")]
public sealed class RabbitMQConsumerCapacityTests(RabbitMQFixture fixture)
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Consumer_Should_UseRemainingCapacity_When_OneLostHandlerIgnoresCancellation()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false,
            cancellationToken: TestContext.Current.CancellationToken);
        using var loggerFactory = LoggerFactory.Create(_ => { });
        await using var manager = new RabbitMQConsumerManager(
            loggerFactory.CreateLogger<RabbitMQConsumerManager>(),
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            "orders", gate.HandleAsync, prefetchCount: 2, consumerDispatchConcurrency: 2,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order-0"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order-1"u8.ToArray(), TestContext.Current.CancellationToken);
        Assert.True(await Condition.WaitAsync(() => gate.Delivered.Count == 1 && gate.ActiveHandlers == 1, s_timeout));

        try
        {
            // act
            var lostChannel = registration.Channel!;
            await lostChannel.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var redelivered = await Condition.WaitAsync(
                () => gate.Delivered.Count == 2 && gate.CancelledHandlers == 1 && gate.ActiveHandlers == 1,
                s_timeout);

            // assert
            new
            {
                Redelivered = redelivered,
                gate.ActiveHandlers,
                gate.PeakConcurrency,
                gate.Invocations,
                manager.IsHealthy,
                Delivered = gate.Delivered.Order().ToArray()
            }.MatchInlineSnapshot(
                """
                {
                  "Redelivered": true,
                  "ActiveHandlers": 1,
                  "PeakConcurrency": 2,
                  "Invocations": 3,
                  "IsHealthy": true,
                  "Delivered": [
                    "order-0",
                    "order-1"
                  ]
                }
                """);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

    [Fact]
    public async Task Consumer_Should_KeepConcurrencyBudget_When_ChannelsAreLostRepeatedly()
    {
        // arrange
        await using var vhost = await fixture.CreateVhostAsync();
        await using var connection = await vhost.ConnectionFactory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("orders", durable: true, exclusive: false, autoDelete: false, cancellationToken: TestContext.Current.CancellationToken);
        using var logs = new LogRecorder();
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        await using var manager = new RabbitMQConsumerManager(
            loggerFactory.CreateLogger<RabbitMQConsumerManager>(),
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 2);
        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            "orders", gate.HandleAsync, prefetchCount: 4, consumerDispatchConcurrency: 2,
            TestContext.Current.CancellationToken);
        for (var i = 0; i < 4; i++)
        {
            await channel.BasicPublishAsync("", "orders", Encoding.UTF8.GetBytes($"order-{i}"), TestContext.Current.CancellationToken);
        }

        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        try
        {
            // act
            var recoveries = new List<object>();
            for (var i = 0; i < 6; i++)
            {
                var lostChannel = registration.Channel!;
                await lostChannel.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
                var reattached = await Condition.WaitAsync(
                    () => registration.IsAttached && !ReferenceEquals(registration.Channel, lostChannel), s_timeout);
                recoveries.Add(new { Reattached = reattached, gate.Invocations, gate.ActiveHandlers, manager.IsHealthy });
            }

            var cancelled = await Condition.WaitAsync(() => gate.CancelledHandlers == 2, s_timeout);
            gate.Release.TrySetResult();
            var delivered = await Condition.WaitAsync(() => gate.Delivered.Count == 4 && manager.IsHealthy, s_timeout);

            // assert
            new
            {
                Recoveries = recoveries,
                CancellationObserved = cancelled,
                AllMessagesDelivered = delivered,
                Delivered = gate.Delivered.Order().ToArray(),
                gate.PeakConcurrency,
                RecoveryBlockedLogged = logs.Contains("ConsumerRecoveryBlocked"),
                RecoveryResumedLogged = logs.Contains("ConsumerRecoveryResumed")
            }.MatchInlineSnapshot(
                """
                {
                  "Recoveries": [
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    },
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    },
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    },
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    },
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    },
                    {
                      "Reattached": true,
                      "Invocations": 2,
                      "ActiveHandlers": 2,
                      "IsHealthy": false
                    }
                  ],
                  "CancellationObserved": true,
                  "AllMessagesDelivered": true,
                  "Delivered": [
                    "order-0",
                    "order-1",
                    "order-2",
                    "order-3"
                  ],
                  "PeakConcurrency": 2,
                  "RecoveryBlockedLogged": true,
                  "RecoveryResumedLogged": true
                }
                """);
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
        using var loggerFactory = LoggerFactory.Create(_ => { });
        await using var manager = new RabbitMQConsumerManager(
            loggerFactory.CreateLogger<RabbitMQConsumerManager>(),
            async ct => await vhost.ConnectionFactory.CreateConnectionAsync(ct));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var gate = new HandlerGate(blockedHandlers: 1);
        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            "orders", gate.HandleAsync, prefetchCount: 1, consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        await channel.BasicPublishAsync("", "orders", "order"u8.ToArray(), TestContext.Current.CancellationToken);
        await gate.Started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        try
        {
            // act
            var lostChannel = registration.Channel!;
            await lostChannel.BasicAckAsync(9999, multiple: false, TestContext.Current.CancellationToken);
            var reattached = await Condition.WaitAsync(
                () => registration.IsAttached && !ReferenceEquals(registration.Channel, lostChannel), s_timeout);
            using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await registration.StopAsync(deadline.Token).WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            var activeAtStop = gate.ActiveHandlers;
            gate.Release.TrySetResult();
            var finished = await Condition.WaitAsync(() => gate.ActiveHandlers == 0, s_timeout);

            // assert
            new { Reattached = reattached, ActiveAtStop = activeAtStop, Finished = finished, gate.Invocations, registration.IsAttached }
                .MatchInlineSnapshot(
                    """
                    {
                      "Reattached": true,
                      "ActiveAtStop": 1,
                      "Finished": true,
                      "Invocations": 1,
                      "IsAttached": false
                    }
                    """);
        }
        finally
        {
            gate.Release.TrySetResult();
        }
    }

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
