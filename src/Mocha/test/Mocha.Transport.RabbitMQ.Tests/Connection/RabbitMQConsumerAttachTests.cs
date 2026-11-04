using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQConsumerAttachTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task StopAsync_Should_CancelAttach_When_ConnectionIsAttachingConsumer()
    {
        // arrange
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var broker = new StubBroker();
        broker.OnConsume = async (attach, _) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(attach.CancellationToken);
        };
        await using var manager = broker.CreateManager();
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        var connect = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        var stop = registration.StopAsync(new CancellationToken(canceled: true));
        try
        {
            await stop.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            release.TrySetResult();
            await Task.WhenAll(connect, stop);
        }

        // assert
        Assert.False(registration.IsAttached);
        Assert.Equal(1, broker.ConsumeAttempts("orders"));
        Assert.True(manager.IsConnected);
    }

    [Fact]
    public async Task Consumer_Should_Reattach_When_ChannelIsLostAsReattachCompletes()
    {
        // arrange
        var broker = new StubBroker();
        IAsyncBasicConsumer? firstConsumer = null;
        broker.OnConsume = (attach, consumer) =>
        {
            firstConsumer ??= consumer;
            if (attach.Attempt == 2)
            {
                attach.Channel.CloseAfterNextOpenCheck();
            }

            return Task.CompletedTask;
        };
        await using var manager = await broker.ConnectAsync();
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        await firstConsumer!.HandleBasicCancelAsync("orders-1", TestContext.Current.CancellationToken);
        var recovered = await Condition.WaitAsync(
            () => broker.ConsumeAttempts("orders") == 3 && registration.IsAttached,
            s_timeout);

        // assert
        Assert.True(recovered);
        Assert.Equal("orders-3", registration.ConsumerTag);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_Reattach_When_BrokerCancelsBeforeConsumeReturns()
    {
        // arrange
        var broker = new StubBroker();
        broker.OnConsume = static (attach, consumer) =>
            attach.Attempt == 1 ? consumer.HandleBasicCancelAsync(attach.ConsumerTag) : Task.CompletedTask;
        await using var manager = await broker.ConnectAsync();

        // act
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        var reattached = await Condition.WaitAsync(() => broker.ConsumeAttempts("orders") == 2, s_timeout);

        // assert
        Assert.True(reattached);
        Assert.True(registration.IsAttached);
        Assert.Equal("orders-2", registration.ConsumerTag);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_Reattach_When_ChannelClosesBeforeConsumeReturns()
    {
        // arrange
        var broker = new StubBroker();
        broker.OnConsume = static (attach, _) =>
            attach.Attempt == 1
                ? attach.Channel.CloseByBrokerAsync(406, "PRECONDITION_FAILED - delivery acknowledgement timed out")
                : Task.CompletedTask;
        await using var manager = await broker.ConnectAsync();

        // act
        var registration = await manager.RegisterConsumerAsync(
            "orders",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        var reattached = await Condition.WaitAsync(() => broker.ConsumeAttempts("orders") == 2, s_timeout);

        // assert
        Assert.True(reattached);
        Assert.True(registration.IsAttached);
        Assert.Equal("orders-2", registration.ConsumerTag);
    }

    [Fact]
    public async Task ReconnectConsumers_Should_AttachOtherConsumers_When_OneAttachNeverCompletes()
    {
        // arrange
        var broker = new StubBroker();
        broker.OnConsume = static (attach, _) =>
            attach is { Queue: "stuck", Attempt: 1 }
                ? Task.Delay(Timeout.Infinite, attach.CancellationToken)
                : Task.CompletedTask;
        await using var manager = broker.CreateManager();
        manager.AttachTimeout = TimeSpan.FromMilliseconds(500);
        var stuck = await manager.RegisterConsumerAsync(
            "stuck",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);
        var orders = await manager.RegisterConsumerAsync(
            "orders",
            provisionAsync: null,
            static (_, _, _) => default,
            prefetchCount: 1,
            consumerDispatchConcurrency: 1,
            TestContext.Current.CancellationToken);

        // act
        // the first connection attaches every registered consumer, and gives up on the stuck one after the timeout
        var stopwatch = Stopwatch.StartNew();
        var connect = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var connected = await Task.WhenAny(connect, Task.Delay(s_timeout, TestContext.Current.CancellationToken))
            == connect;
        var connectedWithinAttachTimeout = stopwatch.Elapsed < TimeSpan.FromSeconds(2);
        var attached = await Condition.WaitAsync(() => orders.IsAttached && stuck.IsAttached, s_timeout);

        // assert
        Assert.True(connected);
        Assert.True(connectedWithinAttachTimeout);
        Assert.True(attached);
        Assert.Equal(1, broker.ConsumeAttempts("orders"));
        Assert.Equal(2, broker.ConsumeAttempts("stuck"));
    }

    /// <summary>
    /// A connection whose channels accept every consumer, and that lets a test interfere with an attach.
    /// </summary>
    private sealed class StubBroker
    {
        private readonly ConcurrentDictionary<string, int> _consumeAttempts = new();
        private readonly Mock<IConnection> _connection = new();

        public StubBroker()
        {
            _connection.SetupGet(c => c.IsOpen).Returns(true);
            _connection
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new StubChannel(this).Object);
        }

        public Func<Attach, IAsyncBasicConsumer, Task> OnConsume { get; set; } =
            static (_, _) => Task.CompletedTask;

        public int ConsumeAttempts(string queue) => _consumeAttempts.GetValueOrDefault(queue);

        public RabbitMQConsumerManager CreateManager()
            => new(
                NullLogger<RabbitMQConsumerManager>.Instance,
                _ => new ValueTask<IConnection>(_connection.Object));

        public async Task<RabbitMQConsumerManager> ConnectAsync()
        {
            var manager = CreateManager();
            await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
            return manager;
        }

        private int NextAttempt(string queue) => _consumeAttempts.AddOrUpdate(queue, 1, static (_, count) => count + 1);

        public sealed record Attach(string Queue, int Attempt, StubChannel Channel, CancellationToken CancellationToken)
        {
            public string ConsumerTag => $"{Queue}-{Attempt}";
        }

        public sealed class StubChannel
        {
            private readonly List<AsyncEventHandler<ShutdownEventArgs>> _shutdownHandlers = [];
            private readonly Mock<IChannel> _channel = new();
            private bool _isOpen = true;
            private bool _closeAfterNextOpenCheck;

            public StubChannel(StubBroker broker)
            {
                _channel.SetupGet(c => c.IsOpen).Returns(() =>
                {
                    var isOpen = _isOpen;
                    if (_closeAfterNextOpenCheck)
                    {
                        _isOpen = false;
                        _closeAfterNextOpenCheck = false;
                    }

                    return isOpen;
                });
                _channel
                    .SetupAdd(c => c.ChannelShutdownAsync += It.IsAny<AsyncEventHandler<ShutdownEventArgs>>())
                    .Callback<AsyncEventHandler<ShutdownEventArgs>>(_shutdownHandlers.Add);
                _channel
                    .Setup(c => c.BasicConsumeAsync(
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>(),
                        It.IsAny<bool>(),
                        It.IsAny<IDictionary<string, object?>?>(),
                        It.IsAny<IAsyncBasicConsumer>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(async (
                        string queue,
                        bool _,
                        string _,
                        bool _,
                        bool _,
                        IDictionary<string, object?>? _,
                        IAsyncBasicConsumer consumer,
                        CancellationToken cancellationToken) =>
                    {
                        var attach = new Attach(queue, broker.NextAttempt(queue), this, cancellationToken);
                        await broker.OnConsume(attach, consumer);
                        return attach.ConsumerTag;
                    });
            }

            public IChannel Object => _channel.Object;

            public void CloseAfterNextOpenCheck() => _closeAfterNextOpenCheck = true;

            public async Task CloseByBrokerAsync(ushort replyCode, string replyText)
            {
                _isOpen = false;
                var reason = new ShutdownEventArgs(ShutdownInitiator.Peer, replyCode, replyText);

                foreach (var handler in _shutdownHandlers)
                {
                    await handler(_channel.Object, reason);
                }
            }
        }
    }
}
