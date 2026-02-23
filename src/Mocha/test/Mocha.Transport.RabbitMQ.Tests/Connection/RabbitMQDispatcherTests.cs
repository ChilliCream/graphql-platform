using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQDispatcherTests
{
    [Fact]
    public async Task RentChannelAsync_Should_CreateNewChannel_When_PoolEmpty()
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
    public async Task RentChannelAsync_Should_ReuseChannel_When_PoolHasOpenChannel()
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
    public async Task RentChannelAsync_Should_SkipClosedChannels_When_PoolHasClosedChannel()
    {
        // arrange
        var closedChannelMock = CreateOpenChannel();
        var freshChannelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(closedChannelMock, freshChannelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(closedChannelMock.Object, rented);

        // Simulate the channel closing while in the pool
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
    public async Task ReturnChannelAsync_Should_AddToPool_When_ChannelOpenAndPoolNotFull()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert — renting again should return the same pooled channel
        var second = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(channelMock.Object, second);
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_PoolFull()
    {
        // arrange
        var channelMocks = Enumerable.Range(0, 11).Select(_ => CreateOpenChannel()).ToArray();
        var connectionMock = CreateOpenConnection(channelMocks);
        await using var dispatcher = CreateDispatcher(connectionMock);

        // Rent all 11 channels
        var rented = new IChannel[11];
        for (var i = 0; i < 11; i++)
        {
            rented[i] = await dispatcher.RentChannelAsync(CancellationToken.None);
        }

        // Return first 10 — fills the pool
        for (var i = 0; i < 10; i++)
        {
            await dispatcher.ReturnChannelAsync(rented[i]);
        }

        // act — return the 11th, which should be disposed (pool full)
        await dispatcher.ReturnChannelAsync(rented[10]);

        // assert — the 11th channel should have been disposed
        channelMocks[10].Verify(c => c.DisposeAsync(), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_ChannelClosed()
    {
        // arrange
        var channelMock = CreateOpenChannel();
        var connectionMock = CreateOpenConnection(channelMock);
        await using var dispatcher = CreateDispatcher(connectionMock);

        var rented = await dispatcher.RentChannelAsync(default);
        channelMock.SetupGet(c => c.IsOpen).Returns(false);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        channelMock.Verify(c => c.DisposeAsync(), Times.AtLeastOnce());
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
    public async Task DisposeAsync_Should_ClearAllChannels_When_Called()
    {
        // arrange
        var channelMocks = Enumerable.Range(0, 3).Select(_ => CreateOpenChannel()).ToArray();
        var connectionMock = CreateOpenConnection(channelMocks);
        var dispatcher = CreateDispatcher(connectionMock);

        // Rent all channels first, then return them to populate pool
        var rented = new IChannel[3];
        for (var i = 0; i < 3; i++)
        {
            rented[i] = await dispatcher.RentChannelAsync(CancellationToken.None);
        }

        for (var i = 0; i < 3; i++)
        {
            await dispatcher.ReturnChannelAsync(rented[i]);
        }

        // act
        await dispatcher.DisposeAsync();

        // assert — all channels should have been disposed
        foreach (var ch in channelMocks)
        {
            ch.Verify(c => c.DisposeAsync(), Times.AtLeastOnce());
        }
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
