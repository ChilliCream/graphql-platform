using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQConnectionManagerTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

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
        await entered.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // act
        await manager.DisposeAsync().AsTask().WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => starting);
        Assert.False(manager.IsConnected);
    }

    [Fact]
    public async Task StartAsync_Should_TimeOut_When_ConnectionAttemptDoesNotComplete()
    {
        // arrange
        await using var manager = CreateManager(NeverConnectAsync);
        manager.ConnectTimeout = TimeSpan.FromMilliseconds(100);

        // act
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => manager.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_timeout, TestContext.Current.CancellationToken));

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
        manager.ConnectTimeout = TimeSpan.FromMilliseconds(100);

        // act
        await Assert.ThrowsAsync<TimeoutException>(
            () => manager.StartAsync(TestContext.Current.CancellationToken).WaitAsync(s_timeout, TestContext.Current.CancellationToken));

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
}
