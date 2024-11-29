using System.Text;
using Moq;

namespace StrawberryShake.Transport.WebSockets;

public class MessagePipelineTests
{
    [Fact]
    public async Task Constructor_AllArgs_Construct()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        ISocketClient socketClient = new SocketClientStub() { IsClosed = false, };

        // act
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // assert
        Assert.IsType<MessagePipeline>(messagePipeline);
    }

    [Fact]
    public async Task Start_NeverCalled_NotStarted()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        Mock<ISocketClient> socketClientMock = new(MockBehavior.Strict);
        var socketClient = socketClientMock.Object;

        // act
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // assert
        socketClientMock.VerifyAll();
    }

    [Fact]
    public async Task Start_StartOnce_StartDataReceive()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        SocketClientStub socketClient = new() { IsClosed = false, };

        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // act
        messagePipeline.Start();
        await socketClient.WaitTillFinished();

        // assert
        Assert.Equal(1, socketClient.GetCallCount(x => x.ReceiveAsync(default!, default!)));
    }

    [Fact]
    public async Task Start_StartMultipleTimes_StartsOnlyOnce()
    {
        // arrange
        SocketClientStub socketClient = new() { IsClosed = false, };
        ProcessAsync a = (_, _) => default;
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // act
        messagePipeline.Start();
        messagePipeline.Start();
        messagePipeline.Start();
        messagePipeline.Start();
        messagePipeline.Start();
        await socketClient.WaitTillFinished();

        // assert
        Assert.Equal(1, socketClient.GetCallCount(x => x.ReceiveAsync(default!, default!)));
    }

    [Fact]
    public async Task Start_StartOnce_WireUpProcessAsync()
    {
        // arrange
        var processed = new SemaphoreSlim(0);
        string? result = null;
        ProcessAsync a = (a, _) =>
        {
            result = Encoding.UTF8.GetString(a.FirstSpan);
            processed.Release();
            return default;
        };
        SocketClientStub socketClient = new() { IsClosed = false, };
        socketClient.MessagesReceive.Enqueue("ab");
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // act
        messagePipeline.Start();
        await socketClient.WaitTillFinished();
        await processed.WaitAsync();

        // assert
        Assert.Equal("ab", result);
    }

    [Fact]
    public async Task Stop_StopOnce_StopDataReceive()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        SocketClientStub socketClient = new() { IsClosed = false, };
        socketClient.MessagesReceive.Enqueue("ab");
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // act
        messagePipeline.Start();
        await messagePipeline.Stop();

        // assert
        Assert.True(socketClient.LatestCancellationToken.IsCancellationRequested);
    }

    [Fact(Skip = "This test is flaky")]
    public async Task Stop_StopMultipleTimes_StopsOnlyOnce()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        SocketClientStub socketClient = new() { IsClosed = false, };
        socketClient.MessagesReceive.Enqueue("ab");
        await using var messagePipeline = new MessagePipeline(socketClient, a);

        // act
        messagePipeline.Start();
        await messagePipeline.Stop();
        await messagePipeline.Stop();
        await messagePipeline.Stop();
        await messagePipeline.Stop();

        // assert
        Assert.True(socketClient.LatestCancellationToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Start_Dispose_CallsStop()
    {
        // arrange
        ProcessAsync a = (_, _) => default;
        SocketClientStub socketClient = new() { IsClosed = false, };
        socketClient.MessagesReceive.Enqueue("ab");

        // act
        await using (var messagePipeline = new MessagePipeline(socketClient, a))
        {
            messagePipeline.Start();
            await socketClient.WaitTillFinished();
        }

        // assert
        Assert.Equal(2, socketClient.GetCallCount(x => x.ReceiveAsync(default!, default!)));
        Assert.True(socketClient.LatestCancellationToken.IsCancellationRequested);
    }
}
