using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQConnectionManagerTests
{
    [Fact]
    public async Task GetConnectionAsync_Should_ThrowObjectDisposed_When_Disposed()
    {
        // arrange
        var manager = CreateManager(NeverConnectAsync);
        await manager.DisposeAsync();

        // act
        var exception = await Record.ExceptionAsync(
            () => manager.GetConnectionAsync(TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public async Task DisposeAsync_Should_FailPendingStart_When_ConnectionAttemptIsPending()
    {
        // arrange
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = CreateManager(
            async ct =>
            {
                entered.TrySetResult();
                return await NeverConnectAsync(ct);
            });
        var starting = manager.StartAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync().AsTask().WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => starting);
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task StartAsync_Should_TimeOut_When_ConnectionAttemptDoesNotComplete()
    {
        // arrange
        await using var manager = CreateManager(NeverConnectAsync);
        manager.StartTimeout = TimeSpan.FromMilliseconds(100);

        // act
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => manager.StartAsync(TestContext.Current.CancellationToken).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("Connecting to RabbitMQ did not complete within 00:00:00.1000000.", exception.Message);
        Assert.False(TestContext.Current.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task StartAsync_Should_DisposeConnection_When_SetUpDoesNotComplete()
    {
        // arrange
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(true);
        await using var manager = CreateManager(
            _ => new ValueTask<IConnection>(connection.Object),
            static (_, ct) => Task.Delay(Timeout.InfiniteTimeSpan, ct));
        manager.StartTimeout = TimeSpan.FromMilliseconds(100);

        // act
        await Assert.ThrowsAsync<TimeoutException>(
            () => manager.StartAsync(TestContext.Current.CancellationToken).WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));

        // assert
        connection.Verify(c => c.DisposeAsync(), Times.Once);
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeConnection_When_CloseFails()
    {
        // arrange
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(true);
        connection
            .Setup(c => c.CloseAsync(
                It.IsAny<ushort>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("The socket is gone."));
        var manager = CreateManager(_ => new ValueTask<IConnection>(connection.Object));
        await manager.StartAsync(TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync();

        // assert
        connection.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData(1, 1)]
    public async Task StartAsync_Should_CapRetryDelayAtMaxRetryDelay_When_AttemptsKeepFailing(
        int? maxRetryDelaySeconds,
        int expectedCapSeconds)
    {
        // arrange
        const int failedAttempts = 8;
        var cap = TimeSpan.FromSeconds(expectedCapSeconds);
        var time = new RecordingTimeProvider();
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(true);
        var attempts = 0;
        await using var manager = new RabbitMQConnectionManager(
            NullLogger.Instance,
            "test",
            time,
            _ => Interlocked.Increment(ref attempts) <= failedAttempts
                ? ValueTask.FromException<IConnection>(new IOException("The broker is unreachable."))
                : new ValueTask<IConnection>(connection.Object),
            static (_, _) => Task.CompletedTask);
        if (maxRetryDelaySeconds is { } seconds)
        {
            manager.MaxRetryDelay = TimeSpan.FromSeconds(seconds);
        }

        // act
        await manager.StartAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);

        // assert
        // a delay lies between half of its ceiling and its ceiling, and the ceiling of the last one is the cap
        var delays = time.Delays.ToArray();
        Assert.Equal(failedAttempts, delays.Length);
        Assert.All(delays, delay => Assert.InRange(delay, TimeSpan.Zero, cap));
        Assert.InRange(delays[^1], cap / 2, cap);
    }

    [Theory]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(TimeoutException))]
    public async Task CloseChannelAsync_Should_DisposeChannel_When_CloseFails(Type failure)
    {
        // arrange
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel
            .Setup(c => c.CloseAsync(
                It.IsAny<ushort>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync((Exception)Activator.CreateInstance(failure)!);

        // act
        await RabbitMQConnectionManager.CloseChannelAsync(channel.Object);

        // assert
        channel.Verify(c => c.DisposeAsync(), Times.Once);
    }

    private static RabbitMQConnectionManager CreateManager(
        Func<CancellationToken, ValueTask<IConnection>> connectionFactory,
        Func<IConnection, CancellationToken, Task>? setUpConnectionAsync = null)
        => new(
            NullLogger.Instance,
            "test",
            TimeProvider.System,
            connectionFactory,
            setUpConnectionAsync ?? (static (_, _) => Task.CompletedTask));

    private static async ValueTask<IConnection> NeverConnectAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new UnreachableException();
    }

    /// <summary>
    /// A clock whose timers are due at once, and that records the delay each timer was created with.
    /// </summary>
    private sealed class RecordingTimeProvider : TimeProvider
    {
        public ConcurrentQueue<TimeSpan> Delays { get; } = new();

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                return base.CreateTimer(callback, state, dueTime, period);
            }

            Delays.Enqueue(dueTime);
            return base.CreateTimer(callback, state, TimeSpan.Zero, period);
        }
    }
}
