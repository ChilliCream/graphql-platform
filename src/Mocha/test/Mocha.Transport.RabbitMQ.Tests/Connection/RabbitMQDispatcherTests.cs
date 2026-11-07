using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQDispatcherTests
{
    [Fact]
    public async Task RentChannelAsync_Should_ShareChannel_When_PublisherHasConcurrentLeases()
    {
        // arrange
        var connection = CreateOpenConnection();
        connection.SetupGet(c => c.ChannelMax).Returns(1);
        await using var dispatcher = CreateDispatcher(connection);
        var publisher = new object();

        // act
        var first = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        var other = dispatcher.RentChannelAsync(new object(), TestContext.Current.CancellationToken).AsTask();
        await dispatcher.ReturnChannelAsync(first);
        var wasWaiting = !other.IsCompleted;
        await dispatcher.ReturnChannelAsync(second);
        var next = await other.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(next);

        // assert
        Assert.Same(first, second);
        Assert.True(wasWaiting);
        Assert.Same(first, next);
        connection.Verify(c => c.CreateChannelAsync(
            It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task RentChannelAsync_Should_IsolateChannels_When_PublishersDiffer()
    {
        // arrange
        var connection = CreateOpenConnection();
        await using var dispatcher = CreateDispatcher(connection);

        // act
        var first = await dispatcher.RentChannelAsync(new object(), TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(new object(), TestContext.Current.CancellationToken);
        var exclusive = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(first);
        await dispatcher.ReturnChannelAsync(second);
        await dispatcher.ReturnChannelAsync(exclusive);

        // assert
        Assert.NotSame(first, second);
        Assert.NotSame(first, exclusive);
        Assert.NotSame(second, exclusive);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ShareChannel_When_WaitingPublisherGetsCapacity()
    {
        // arrange
        var connection = CreateOpenConnection();
        connection.SetupGet(c => c.ChannelMax).Returns(1);
        await using var dispatcher = CreateDispatcher(connection);
        var occupied = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        var publisher = new object();
        var first = dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken).AsTask();
        var second = dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken).AsTask();

        // act
        await dispatcher.ReturnChannelAsync(occupied);
        var channels = await Task.WhenAll(first, second)
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        foreach (var channel in channels)
        {
            await dispatcher.ReturnChannelAsync(channel);
        }

        // assert
        Assert.Same(channels[0], channels[1]);
    }

    [Fact]
    public async Task RentChannelAsync_Should_PreserveOtherLease_When_SharedChannelCreationWaitIsCancelled()
    {
        // arrange
        var connection = CreateOpenConnection();
        var opening = new TaskCompletionSource<IChannel>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(opening.Task);
        await using var dispatcher = CreateDispatcher(connection);
        var publisher = new object();
        using var cancellation = new CancellationTokenSource();
        var cancelled = dispatcher.RentChannelAsync(publisher, cancellation.Token).AsTask();
        var waiting = dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken).AsTask();

        // act
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        var channel = CreateOpenChannel();
        opening.SetResult(channel.Object);
        var rented = await waiting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        Assert.Same(channel.Object, rented);
        connection.Verify(c => c.CreateChannelAsync(
            It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task RentChannelAsync_Should_WaitForCapacity_When_OutstandingPublishLimitReached()
    {
        // arrange
        var connection = CreateOpenConnection();
        await using var dispatcher = CreateDispatcher(connection);
        var publisher = new object();
        var channels = new IChannel[1024];
        for (var i = 0; i < channels.Length; i++)
        {
            channels[i] = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        }

        // act
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.RentChannelAsync(publisher, timeout.Token).AsTask());
        await dispatcher.ReturnChannelAsync(channels[0]);
        var next = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(next);
        for (var i = 1; i < channels.Length; i++)
        {
            await dispatcher.ReturnChannelAsync(channels[i]);
        }

        // assert
        Assert.Same(channels[0], next);
        connection.Verify(c => c.CreateChannelAsync(
            It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_PreserveReplacement_When_OldSharedLeaseReturns()
    {
        // arrange
        var old = CreateOpenChannel();
        var replacement = CreateOpenChannel();
        var connection = CreateOpenConnection(old, replacement);
        connection.SetupGet(c => c.ChannelMax).Returns(1);
        await using var dispatcher = CreateDispatcher(connection);
        var publisher = new object();
        var first = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        var second = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        old.SetupGet(c => c.IsOpen).Returns(false);
        dispatcher.DiscardChannel(first);
        var current = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);

        // act
        dispatcher.DiscardChannel(second);
        await dispatcher.ReturnChannelAsync(current);
        var next = await dispatcher.RentChannelAsync(publisher, TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(next);

        // assert
        Assert.Same(replacement.Object, current);
        Assert.Same(current, next);
        replacement.Verify(c => c.DisposeAsync(), Times.Never());
    }

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
