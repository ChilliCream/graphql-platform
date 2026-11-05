using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQDispatcherTests
{
    [Fact]
    public async Task RentChannelAsync_Should_CreateNewChannel_When_NoChannelExists()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        // act
        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);

        // assert
        Assert.Same(channelMock.Object, rented);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ReuseChannel_When_ChannelIsOpen()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var first = await dispatcher.RentChannelAsync(CancellationToken.None);
        await dispatcher.ReturnChannelAsync(first);

        // act
        var second = await dispatcher.RentChannelAsync(CancellationToken.None);

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ReplaceChannel_When_ChannelIsClosed()
    {
        // arrange
        var closedChannelMock = CreateOpenChannel();
        var freshChannelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(closedChannelMock, freshChannelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(closedChannelMock.Object, rented);

        // Simulate a broker-initiated channel loss.
        closedChannelMock.SetupGet(c => c.IsOpen).Returns(false);
        await dispatcher.ReturnChannelAsync(rented);

        // act
        var next = await dispatcher.RentChannelAsync(CancellationToken.None);

        // assert
        Assert.Same(freshChannelMock.Object, next);
    }

    [Fact]
    public async Task RentChannelAsync_Should_UseReplacementConnection_When_OldConnectionRecoversBeforeCleanup()
    {
        // arrange
        var oldChannel = CreateOpenChannel();
        var newChannel = CreateOpenChannel();
        var oldConnection = CreateOpenConnection(oldChannel);
        var newConnection = CreateOpenConnection(newChannel);
        oldConnection.Setup(c => c.DisposeAsync()).Callback(() =>
        {
            oldConnection.SetupGet(c => c.IsOpen).Returns(false);
            oldChannel.SetupGet(c => c.IsOpen).Returns(false);
            oldConnection
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ObjectDisposedException("old connection"));
        }).Returns(ValueTask.CompletedTask);
        var connections = new Queue<IConnection>([oldConnection.Object, newConnection.Object]);
        await using var dispatcher = new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
            _ => new ValueTask<IConnection>(connections.Dequeue()),
            static (_, _) => Task.CompletedTask);
        var initial = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(initial);

        // The old connection is closed for both readiness checks and open again during cleanup.
        var reads = 0;
        oldConnection.SetupGet(c => c.IsOpen).Returns(() => Interlocked.Increment(ref reads) >= 3);

        // act
        await dispatcher.EnsureConnectedAsync(TestContext.Current.CancellationToken);
        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        Assert.Same(newChannel.Object, rented);
        oldChannel.Verify(c => c.DisposeAsync(), Times.Once());
        newConnection.Verify(
            c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task RentChannelAsync_Should_ThrowObjectDisposed_When_Disposed()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var dispatcher = CreateDispatcher(connectionMock);
        await dispatcher.DisposeAsync();

        // act & assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            dispatcher.RentChannelAsync(CancellationToken.None).AsTask()
        );
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_KeepChannel_When_ChannelIsOpen()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        var second = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(channelMock.Object, second);
    }

    [Fact]
    public async Task DisposeAsync_Should_CancelChannelCreation_When_ChannelIsOpening()
    {
        // arrange
        var connection = CreateOpenConnection();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateChannelOptions? _, CancellationToken ct) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return CreateOpenChannel().Object;
            });
        var dispatcher = CreateDispatcher(connection);
        var rent = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // act
        await dispatcher.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rent);
        Assert.False(dispatcher.IsConnected);
    }

    [Fact]
    public async Task DisposeAsync_Should_ClosePooledChannel_When_Called()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        var dispatcher = CreateDispatcher(connection);
        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(rented);

        // act
        await dispatcher.DisposeAsync();

        // assert
        channel.Verify(c => c.DisposeAsync(), Times.Once());
    }

    [Fact]
    public async Task OnConnectionEstablished_Should_InvokeCallback_When_ConnectionCreated()
    {
        // arrange
        var connectionMock = CreateOpenConnection();
        var callbackInvoked = false;
        IConnection? receivedConnection = null;

        await using var dispatcher = new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
            _ => new ValueTask<IConnection>(connectionMock.Object),
            (conn, _) =>
            {
                callbackInvoked = true;
                receivedConnection = conn;
                return Task.CompletedTask;
            });

        // act
        await dispatcher.EnsureConnectedAsync(CancellationToken.None);

        // assert
        Assert.True(callbackInvoked);
        Assert.Same(connectionMock.Object, receivedConnection);
    }

    private static Mock<IChannel> CreateOpenChannel()
    {
        var channelMock = new Mock<IChannel>();
        channelMock.SetupGet(c => c.IsOpen).Returns(true);
        return channelMock;
    }

    private static Mock<IConnection> CreateOpenConnection(params Mock<IChannel>[] channelsToReturn)
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.SetupGet(c => c.IsOpen).Returns(true);
        connectionMock.SetupGet(c => c.ClientProvidedName).Returns("test-connection");

        if (channelsToReturn.Length > 0)
        {
            var queue = new ConcurrentQueue<Mock<IChannel>>(channelsToReturn);
            connectionMock
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => queue.TryDequeue(out var channel) ? channel.Object : CreateOpenChannel().Object);
        }
        else
        {
            connectionMock
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateOpenChannel().Object);
        }

        return connectionMock;
    }

    private static RabbitMQDispatcher CreateDispatcher(Mock<IConnection> connectionMock)
    {
        return new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
            _ => new ValueTask<IConnection>(connectionMock.Object),
            (_, _) => Task.CompletedTask);
    }
}
