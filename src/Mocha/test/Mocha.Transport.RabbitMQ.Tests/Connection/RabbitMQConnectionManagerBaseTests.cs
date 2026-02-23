using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQConnectionManagerBaseTests
{
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
        await manager.EnsureConnectedAsync(default);

        // assert
        Assert.True(manager.IsConnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseAndDisposeConnection_When_Connected()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var manager = CreateManager(connectionMock.Object);

        await manager.EnsureConnectedAsync(default);

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
        await manager.EnsureConnectedAsync(default);

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
        await manager.EnsureConnectedAsync(default);

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
        await manager.EnsureConnectedAsync(default);

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
        await manager.EnsureConnectedAsync(default);

        // assert
        Assert.Equal(1, manager.ConnectionEstablishedCount);
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
        public int BeforeConnectionCreatedCount { get; private set; }
        public int AfterConnectionCreatedCount { get; private set; }
        public int ConnectionLostCount { get; private set; }
        public int ConnectionRecoveredCount { get; private set; }
        public int ConnectionEstablishedCount { get; private set; }

        public TestConnectionManager(ILogger logger, Func<CancellationToken, ValueTask<IConnection>> factory)
            : base(logger, factory) { }

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

        protected override Task OnConnectionRecoveredAsync(CancellationToken cancellationToken)
        {
            ConnectionRecoveredCount++;
            return Task.CompletedTask;
        }

        protected override Task OnConnectionEstablished(IConnection connection, CancellationToken cancellationToken)
        {
            ConnectionEstablishedCount++;
            return Task.CompletedTask;
        }
    }
}
