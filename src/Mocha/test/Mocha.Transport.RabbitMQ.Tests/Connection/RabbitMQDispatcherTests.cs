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
    public async Task RentChannelAsync_Should_ShareChannel_When_PublishesAreConcurrent()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        await using var dispatcher = CreateDispatcher(connection);

        // act
        var rented = await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(_ => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()));
        foreach (var item in rented)
        {
            await dispatcher.ReturnChannelAsync(item);
        }

        // assert
        Assert.All(rented, item => Assert.Same(channel.Object, item));
        connection.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task RentChannelAsync_Should_KeepOpeningChannel_When_CallerCancels()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection();
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateChannelOptions? _, CancellationToken ct) =>
            {
                started.TrySetResult(ct);
                await release.Task.WaitAsync(ct);
                return channel.Object;
            });
        await using var dispatcher = CreateDispatcher(connection);
        using var callerCts = new CancellationTokenSource();

        // act
        var first = dispatcher.RentChannelAsync(callerCts.Token).AsTask();
        var creationToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await callerCts.CancelAsync();
        Task<IChannel> second;
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            second = dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask();
        }
        finally
        {
            release.TrySetResult();
        }

        var rented = await second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        Assert.False(creationToken.IsCancellationRequested);
        Assert.Same(channel.Object, rented);
        connection.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task RentChannelAsync_Should_WaitForCleanup_When_FailedChannelIsRetired()
    {
        // arrange
        var firstChannel = CreateOpenChannel();
        var replacement = CreateOpenChannel();
        var cleanup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        firstChannel.Setup(c => c.CloseAsync(
                It.IsAny<ushort>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(cleanup.Task);
        var connection = CreateOpenConnection(firstChannel, replacement);
        await using var dispatcher = CreateDispatcher(connection);
        var first = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);

        // act
        dispatcher.DiscardChannel(first);
        try
        {
            using var waitTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => dispatcher.RentChannelAsync(waitTimeout.Token).AsTask()
                    .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            connection.Verify(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()),
                Times.Once());
        }
        finally
        {
            cleanup.TrySetResult();
        }

        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        Assert.Same(replacement.Object, rented);
        firstChannel.Verify(c => c.DisposeAsync(), Times.Once());
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
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_DispatcherDisposed()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);
        await dispatcher.DisposeAsync();

        // Clear previous calls from dispose
        channelMock.Invocations.Clear();
        channelMock.SetupGet(c => c.IsOpen).Returns(true);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        channelMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce());
    }

    [Fact]
    public async Task DisposeAsync_Should_CloseSharedChannel_When_Called()
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
            var queue = new Queue<Mock<IChannel>>(channelsToReturn);
            connectionMock
                .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (queue.Count == 0)
                    {
                        return CreateOpenChannel().Object;
                    }

                    return queue.Dequeue().Object;
                });
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
