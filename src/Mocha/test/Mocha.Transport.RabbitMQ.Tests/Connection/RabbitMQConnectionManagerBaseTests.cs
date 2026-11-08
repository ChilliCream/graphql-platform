using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQConnectionManagerBaseTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetConnectionAsync_Should_WaitForSetup_When_ConnectionIsOpen(bool duringAfterConnectionCreated)
    {
        // arrange
        var connection = CreateOpenConnection();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(connection.Object);
        if (duringAfterConnectionCreated)
        {
            manager.AfterSetupAsync = SetupAsync;
        }
        else
        {
            manager.SetupAsync = SetupAsync;
        }

        var connecting = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // act
        var waiting = manager.GetConnectionAsync(TestContext.Current.CancellationToken).AsTask();
        var waitedForSetup = !waiting.IsCompleted;
        var connectedDuringSetup = manager.IsConnected;
        release.TrySetResult();
        await connecting;
        var current = await waiting;

        // assert
        Assert.True(waitedForSetup);
        Assert.False(connectedDuringSetup);
        Assert.Same(connection.Object, current);
        Assert.True(manager.IsConnected);

        async Task SetupAsync(CancellationToken ct)
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        }
    }

    [Fact]
    public async Task GetConnectionAsync_Should_PreserveSetup_When_ConcurrentCallerCancels()
    {
        // arrange
        var connection = CreateOpenConnection();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var manager = CreateManager(connection.Object);
        manager.SetupAsync = async ct =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(ct);
        };
        var connecting = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // act
        bool setupStillRunning;
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => manager.GetConnectionAsync(deadline.Token).AsTask());
            setupStillRunning = !connecting.IsCompleted;
        }
        finally
        {
            release.TrySetResult();
            await connecting;
        }

        // assert
        Assert.True(setupStillRunning);
        Assert.True(manager.IsConnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetConnectionAsync_Should_TimeOut_When_ConnectionAttemptStalls(bool duringSetup)
    {
        // arrange
        var connection = CreateOpenConnection();
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            async ct =>
            {
                if (!duringSetup)
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }

                return connection.Object;
            });
        manager.ConnectTimeout = TimeSpan.FromMilliseconds(100);
        manager.SetupAsync = ct => duringSetup ? Task.Delay(Timeout.InfiniteTimeSpan, ct) : Task.CompletedTask;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));

        // act
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => manager.GetConnectionAsync(deadline.Token).AsTask());

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception.InnerException);
        Assert.False(deadline.IsCancellationRequested);
        Assert.False(manager.IsConnected);
        connection.Verify(c => c.DisposeAsync(), duringSetup ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task GetConnectionAsync_Should_PreserveCallerCancellation_When_Connecting()
    {
        // arrange
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return CreateOpenConnection().Object;
            });
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => manager.GetConnectionAsync(cancellation.Token).AsTask());

        // assert
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task GetConnectionAsync_Should_AllowFirstAttempt_When_ConnectTimeoutIsZero()
    {
        // arrange
        var connection = CreateOpenConnection();
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            async ct =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
                return connection.Object;
            });
        manager.ConnectTimeout = TimeSpan.Zero;

        // act
        var result = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Same(connection.Object, result);
    }

    [Fact]
    public async Task GetConnectionAsync_Should_CreateConnection_When_NotConnected()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        await using var manager = CreateManager(connectionMock.Object);

        // act
        var result = await manager.GetConnectionAsync(CancellationToken.None);

        // assert
        Assert.Same(connectionMock.Object, result);
    }

    [Fact]
    public async Task GetConnectionAsync_Should_ReuseConnection_When_AlreadyConnected()
    {
        // arrange
        var factoryCallCount = 0;
        var connectionMock = CreateOpenConnection();
        await using var manager = new TestConnectionManager(
            NullLoggerFactory.Instance.CreateLogger<TestConnectionManager>(),
            _ =>
            {
                factoryCallCount++;
                return new ValueTask<IConnection>(connectionMock.Object);
            });

        // act
        var first = await manager.GetConnectionAsync(CancellationToken.None);
        var second = await manager.GetConnectionAsync(CancellationToken.None);

        // assert
        Assert.Same(first, second);
        Assert.Equal(1, factoryCallCount);
    }

    [Fact]
    public async Task GetConnectionAsync_Should_ThrowObjectDisposed_When_Disposed()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var manager = CreateManager(connectionMock.Object);
        await manager.DisposeAsync();

        // act & assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            manager.GetConnectionAsync(CancellationToken.None).AsTask()
        );
    }

    [Fact]
    public async Task EnsureConnectedAsync_Should_CreateConnection_When_NotConnected()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        await using var manager = CreateManager(connectionMock.Object);

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(manager.IsConnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseAndDisposeConnection_When_Connected()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var manager = CreateManager(connectionMock.Object);

        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync();

        // assert
        connectionMock.Verify(c => c.CloseAsync(
            It.IsAny<ushort>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce());
        connectionMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var manager = CreateManager(connectionMock.Object);
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync();
        await manager.DisposeAsync();

        // assert
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task Lifecycle_Should_InvokeBeforeConnectionCreated_When_Connecting()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        await using var manager = CreateManager(connectionMock.Object);

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, manager.BeforeConnectionCreatedCount);
    }

    [Fact]
    public async Task Lifecycle_Should_InvokeAfterConnectionCreated_When_Connecting()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        await using var manager = CreateManager(connectionMock.Object);

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, manager.AfterConnectionCreatedCount);
    }

    [Fact]
    public async Task OnConnectionRecoveredAsync_Should_NotRun_When_FirstSetupAttemptFails()
    {
        // arrange
        var connections = new Queue<IConnection>([CreateOpenConnection().Object, CreateOpenConnection().Object]);
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        manager.ConnectTimeout = TimeSpan.Zero;
        manager.SetupAsync = static _ => throw new InvalidOperationException("Setup failed.");
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => manager.GetConnectionAsync(TestContext.Current.CancellationToken).AsTask());
        manager.SetupAsync = static _ => Task.CompletedTask;

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, manager.ConnectionRecoveredCount);
        Assert.True(manager.IsConnected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnConnectionRecoveredAsync_Should_RunAfterSetup_When_ConnectionReplaced(bool background)
    {
        // arrange
        var first = CreateOpenConnection();
        var second = CreateOpenConnection();
        var connections = new Queue<IConnection>([first.Object, second.Object]);
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var initialRecoveries = manager.ConnectionRecoveredCount;
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var setupsAtRecovery = 0;
        var afterSetupsAtRecovery = 0;
        manager.RecoverySetupAsync = _ =>
        {
            setupsAtRecovery = manager.ConnectionEstablishedCount;
            afterSetupsAtRecovery = manager.AfterConnectionCreatedCount;
            recovered.TrySetResult();
            return Task.CompletedTask;
        };
        first.SetupGet(c => c.IsOpen).Returns(false);

        // act
        if (background)
        {
            manager.Reconnect();
            await recovered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        var current = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, initialRecoveries);
        Assert.Equal(1, manager.ConnectionRecoveredCount);
        Assert.Equal(2, setupsAtRecovery);
        Assert.Equal(2, afterSetupsAtRecovery);
        Assert.Same(second.Object, current);
    }

    [Fact]
    public async Task OnConnectionRecoveredAsync_Should_AllowConnectionAccess_When_CallbackRuns()
    {
        // arrange
        var first = CreateOpenConnection();
        var second = CreateOpenConnection();
        var connections = new Queue<IConnection>([first.Object, second.Object]);
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        IConnection? recoveredConnection = null;
        manager.RecoverySetupAsync = async ct => recoveredConnection = await manager.GetConnectionAsync(ct);
        first.SetupGet(c => c.IsOpen).Returns(false);

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.Same(second.Object, recoveredConnection);
        Assert.True(manager.IsConnected);
    }

    [Fact]
    public async Task Lifecycle_Should_InvokeConnectionEstablished_When_Connecting()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        await using var manager = CreateManager(connectionMock.Object);

        // act
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, manager.ConnectionEstablishedCount);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeConnection_When_CloseFails()
    {
        // arrange
        var connection = CreateOpenConnection();
        connection.Setup(c => c.CloseAsync(
            It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Connection lost."));
        var manager = CreateManager(connection.Object);
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync();

        // assert
        Assert.False(manager.IsConnected);
        connection.Verify(c => c.DisposeAsync(), Times.Once());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DisposeAsync_Should_WaitForCleanup_When_FactoryReturnsAfterCancellation(bool background)
    {
        // arrange
        var connection = CreateOpenConnection();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new TestConnectionManager(NullLogger<TestConnectionManager>.Instance, async _ =>
        {
            started.TrySetResult();
            await release.Task;
            return connection.Object;
        });
        Task? connecting = null;
        if (background)
        {
            manager.Reconnect();
        }
        else
        {
            connecting = manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        }

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // act
        var firstDisposal = manager.DisposeAsync().AsTask();
        var secondDisposal = manager.DisposeAsync().AsTask();
        var waitingForFactory = !firstDisposal.IsCompleted && !secondDisposal.IsCompleted;
        release.TrySetResult();
        await Task.WhenAll(firstDisposal, secondDisposal)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.True(waitingForFactory);
        Assert.False(manager.IsConnected);
        Assert.Equal(0, manager.ConnectionEstablishedCount);
        connection.Verify(c => c.DisposeAsync(), Times.Once());
        if (connecting is not null)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connecting);
        }
    }

    private static Mock<IConnection> CreateOpenConnection()
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.SetupGet(c => c.IsOpen).Returns(true);
        connectionMock.SetupGet(c => c.ClientProvidedName).Returns("test-connection");
        return connectionMock;
    }

    private static TestConnectionManager CreateManager(IConnection connection)
    {
        return new TestConnectionManager(
            NullLoggerFactory.Instance.CreateLogger<TestConnectionManager>(),
            _ => new ValueTask<IConnection>(connection));
    }

    private sealed class TestConnectionManager : RabbitMQConnectionManagerBase
    {
        public Func<CancellationToken, Task> SetupAsync { get; set; } = static _ => Task.CompletedTask;

        public Func<CancellationToken, Task> AfterSetupAsync { get; set; } = static _ => Task.CompletedTask;

        public Func<CancellationToken, Task> RecoverySetupAsync { get; set; } = static _ => Task.CompletedTask;

        public int BeforeConnectionCreatedCount { get; private set; }
        public int AfterConnectionCreatedCount { get; private set; }
        public int ConnectionEstablishedCount { get; private set; }
        public int ConnectionRecoveredCount { get; private set; }

        public TestConnectionManager(ILogger logger, Func<CancellationToken, ValueTask<IConnection>> factory)
            : base(logger, factory) { }

        public void Reconnect() => StartReconnecting();

        protected override Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
        {
            ConnectionRecoveredCount++;
            return RecoverySetupAsync(cancellationToken);
        }

        protected override Task OnBeforeConnectionCreatedAsync(CancellationToken cancellationToken)
        {
            BeforeConnectionCreatedCount++;
            return Task.CompletedTask;
        }

        protected override Task OnAfterConnectionCreatedAsync(
            IConnection connection,
            CancellationToken cancellationToken)
        {
            AfterConnectionCreatedCount++;
            return AfterSetupAsync(cancellationToken);
        }

        protected override Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
        {
            ConnectionEstablishedCount++;
            return SetupAsync(cancellationToken);
        }
    }
}
