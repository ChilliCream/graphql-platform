using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQDispatcherTests
{
    [Fact]
    public async Task RentChannelAsync_Should_CreateNewChannel_When_PoolEmpty()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        await using var dispatcher = CreateDispatcher(connection);

        // act
        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);

        // assert
        Assert.Same(channel, rented);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ReuseChannel_When_PoolHasOpenChannel()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        await using var dispatcher = CreateDispatcher(connection);

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
        var closedChannel = CreateOpenChannel();
        var freshChannel = CreateOpenChannel();
        var connection = CreateOpenConnection(closedChannel, freshChannel);
        await using var dispatcher = CreateDispatcher(connection);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(closedChannel, rented);

        // Simulate the channel closing while in the pool
        closedChannel.IsOpen.Returns(false);
        await dispatcher.ReturnChannelAsync(rented);

        // act
        var next = await dispatcher.RentChannelAsync(CancellationToken.None);

        // assert
        Assert.Same(freshChannel, next);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ThrowObjectDisposed_When_Disposed()
    {
        // arrange
        var connection = CreateOpenConnection();
        var dispatcher = CreateDispatcher(connection);
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
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        await using var dispatcher = CreateDispatcher(connection);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert — renting again should return the same pooled channel
        var second = await dispatcher.RentChannelAsync(CancellationToken.None);
        Assert.Same(channel, second);
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_PoolFull()
    {
        // arrange
        var channels = Enumerable.Range(0, 11).Select(_ => CreateOpenChannel()).ToArray();
        var connection = CreateOpenConnection(channels);
        await using var dispatcher = CreateDispatcher(connection);

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
        await rented[10].Received().DisposeAsync();
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_ChannelClosed()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        await using var dispatcher = CreateDispatcher(connection);

        var rented = await dispatcher.RentChannelAsync(TestContext.Current.CancellationToken);
        channel.IsOpen.Returns(false);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        await channel.Received().DisposeAsync();
    }

    [Fact]
    public async Task ReturnChannelAsync_Should_DisposeChannel_When_DispatcherDisposed()
    {
        // arrange
        var channel = CreateOpenChannel();
        var connection = CreateOpenConnection(channel);
        var dispatcher = CreateDispatcher(connection);

        var rented = await dispatcher.RentChannelAsync(CancellationToken.None);
        await dispatcher.DisposeAsync();

        // Clear previous calls from dispose
        channel.ClearReceivedCalls();
        channel.IsOpen.Returns(true);

        // act
        await dispatcher.ReturnChannelAsync(rented);

        // assert
        await channel.Received().DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_ClearAllChannels_When_Called()
    {
        // arrange
        var channels = Enumerable.Range(0, 3).Select(_ => CreateOpenChannel()).ToArray();
        var connection = CreateOpenConnection(channels);
        var dispatcher = CreateDispatcher(connection);

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
        foreach (var ch in channels)
        {
            await ch.Received().DisposeAsync();
        }
    }

    [Fact]
    public async Task OnConnectionEstablished_Should_InvokeCallback_When_ConnectionCreated()
    {
        // arrange
        var connection = CreateOpenConnection();
        var callbackInvoked = false;
        IConnection? receivedConnection = null;

        await using var dispatcher = new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
            _ => new ValueTask<IConnection>(connection),
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
        Assert.Same(connection, receivedConnection);
    }

    private static IChannel CreateOpenChannel()
    {
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);
        return channel;
    }

    private static IConnection CreateOpenConnection(params IChannel[] channelsToReturn)
    {
        var connection = Substitute.For<IConnection>();
        connection.IsOpen.Returns(true);
        connection.ClientProvidedName.Returns("test-connection");

        if (channelsToReturn.Length > 0)
        {
            var queue = new Queue<IChannel>(channelsToReturn);
            connection
                .CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    if (queue.Count == 0)
                    {
                        return Task.FromResult(CreateOpenChannel());
                    }

                    return Task.FromResult(queue.Dequeue());
                });
        }
        else
        {
            connection
                .CreateChannelAsync(Arg.Any<CreateChannelOptions?>(), Arg.Any<CancellationToken>())
                .Returns(_ => Task.FromResult(CreateOpenChannel()));
        }

        return connection;
    }

    private static RabbitMQDispatcher CreateDispatcher(IConnection connection)
    {
        return new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
            _ => new ValueTask<IConnection>(connection),
            (_, _) => Task.CompletedTask);
    }
}
