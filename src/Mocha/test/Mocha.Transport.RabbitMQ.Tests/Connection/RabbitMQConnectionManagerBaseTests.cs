using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQConnectionManagerBaseTests
{
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
        var exception = await Record.ExceptionAsync(() => manager.GetConnectionAsync(cancellation.Token).AsTask());

        // assert
        Assert.IsAssignableFrom<OperationCanceledException>(exception);
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
    public async Task Reconnect_Should_Retry_When_ConnectionIsLostAsSetupCompletes()
    {
        // arrange
        var lost = CreateOpenConnection();
        var replacement = CreateOpenConnection();
        var connections = new Queue<IConnection>([lost.Object, replacement.Object]);
        await using var manager = new TestConnectionManager(
            NullLogger<TestConnectionManager>.Instance,
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        var wasOpen = true;
        lost.SetupGet(c => c.IsOpen).Returns(() =>
        {
            var result = wasOpen;
            wasOpen = false;
            if (result)
            {
                manager.Reconnect();
            }

            return result;
        });

        // act
        // The loss requests recovery while the preceding recovery still owns its running flag.
        manager.Reconnect();
        var recovered = await Condition.WaitAsync(() => manager.IsConnected, TimeSpan.FromSeconds(5));

        // assert
        Assert.True(recovered);
        Assert.Empty(connections);
        lost.Verify(c => c.DisposeAsync(), Times.Once());
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
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
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
    public async Task ConnectionLost_Should_DisposeConnection_When_ClientRecoveryReopenedIt()
    {
        // arrange
        var recovered = new StubConnection();
        var replacement = new StubConnection();
        var connections = new Queue<IConnection>([recovered.Object, replacement.Object]);
        await using var manager = new TestConnectionManager(
            NullLoggerFactory.Instance.CreateLogger<TestConnectionManager>(),
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        // the client's automatic recovery reopened the connection before the manager handled the loss
        await recovered.RaiseShutdownAsync(keepOpen: true);
        var lost = await Condition.WaitAsync(() => manager.ConnectionLostCount == 1, TimeSpan.FromSeconds(5));
        var current = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(lost);
        Assert.True(recovered.IsDisposed);
        Assert.Same(replacement.Object, current);
    }

    [Fact]
    public async Task ConnectionLost_Should_BeIgnored_When_RaisedByReplacedConnection()
    {
        // arrange
        var replaced = new StubConnection();
        var current = new StubConnection();
        var connections = new Queue<IConnection>([replaced.Object, current.Object]);
        await using var manager = new TestConnectionManager(
            NullLoggerFactory.Instance.CreateLogger<TestConnectionManager>(),
            _ => new ValueTask<IConnection>(connections.Dequeue()));
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        replaced.IsOpen = false;
        await manager.EnsureConnectedAsync(TestContext.Current.CancellationToken);

        // act
        // the client raises the shutdown of the replaced connection after the manager moved on
        await replaced.RaiseShutdownAsync(keepOpen: false);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(0, manager.ConnectionLostCount);
        Assert.False(current.IsDisposed);
        Assert.True(manager.IsConnected);
    }

    [Fact]
    public async Task GetConnectionAsync_Should_Reconnect_When_ConnectionClosedBeforeEventsWired()
    {
        // arrange
        // the broker closed the connection before the manager subscribed, so no shutdown event is raised
        var closed = new StubConnection { IsOpen = false };
        var open = new StubConnection();
        var connections = new Queue<IConnection>([closed.Object, open.Object]);
        await using var manager = new TestConnectionManager(
            NullLoggerFactory.Instance.CreateLogger<TestConnectionManager>(),
            _ => new ValueTask<IConnection>(connections.Dequeue()));

        // act
        var connection = await manager.GetConnectionAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Same(open.Object, connection);
        Assert.True(closed.IsDisposed);
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

    /// <summary>
    /// A connection that records its disposal and lets a test raise its shutdown event.
    /// </summary>
    private sealed class StubConnection
    {
        private readonly List<AsyncEventHandler<ShutdownEventArgs>> _shutdownHandlers = [];
        private readonly Mock<IConnection> _connection = new();

        public StubConnection()
        {
            _connection.SetupGet(c => c.IsOpen).Returns(() => IsOpen);
            _connection.SetupGet(c => c.ClientProvidedName).Returns("test-connection");
            _connection
                .SetupAdd(c => c.ConnectionShutdownAsync += It.IsAny<AsyncEventHandler<ShutdownEventArgs>>())
                .Callback<AsyncEventHandler<ShutdownEventArgs>>(_shutdownHandlers.Add);
            _connection
                .Setup(c => c.DisposeAsync())
                .Callback(() => IsDisposed = true)
                .Returns(ValueTask.CompletedTask);
        }

        public IConnection Object => _connection.Object;

        public bool IsOpen { get; set; } = true;

        public bool IsDisposed { get; private set; }

        public async Task RaiseShutdownAsync(bool keepOpen)
        {
            IsOpen = keepOpen;
            var reason = new ShutdownEventArgs(ShutdownInitiator.Peer, 320, "CONNECTION_FORCED - test");

            foreach (var handler in _shutdownHandlers)
            {
                await handler(_connection.Object, reason);
            }
        }
    }

    private sealed class TestConnectionManager : RabbitMQConnectionManagerBase
    {
        public Func<CancellationToken, Task> SetupAsync { get; set; } = static _ => Task.CompletedTask;

        public int BeforeConnectionCreatedCount { get; private set; }
        public int AfterConnectionCreatedCount { get; private set; }
        public int ConnectionLostCount { get; private set; }
        public int ConnectionEstablishedCount { get; private set; }

        public TestConnectionManager(ILogger logger, Func<CancellationToken, ValueTask<IConnection>> factory)
            : base(logger, factory) { }

        public void Reconnect() => StartReconnecting();

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
            return Task.CompletedTask;
        }

        protected override Task OnConnectionLostAsync()
        {
            ConnectionLostCount++;
            return Task.CompletedTask;
        }

        protected override Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
        {
            ConnectionEstablishedCount++;
            return SetupAsync(cancellationToken);
        }
    }
}
