using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Sockets.Client;

namespace HotChocolate.Fusion.Transport.Sockets;

public class FusionSocketTeardownTests
{
    [Fact]
    public async Task CloseAsync_Should_SendEveryCompleteBeforeClose_When_OperationsAreCompleted()
    {
        // arrange
        using var socket = new TeardownWebSocket();
        var client = await SocketClient.ConnectAsync(
            socket,
            new SocketClientOptions(),
            TestContext.Current.CancellationToken);
        var request = CreateOperationRequest();
        var first = await client.SubscribeAsync(
            request,
            new UnusedArenaSource(),
            TestContext.Current.CancellationToken);
        var second = await client.SubscribeAsync(
            request,
            new UnusedArenaSource(),
            TestContext.Current.CancellationToken);

        // act
        await first.CompleteAsync(TestContext.Current.CancellationToken);
        await first.CompleteAsync(TestContext.Current.CancellationToken);
        await second.CompleteAsync(TestContext.Current.CancellationToken);
        await client.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            ["connection_init", "subscribe", "subscribe", "complete", "complete", "close"],
            socket.Events);
        Assert.True(socket.IsDisposed);
    }

    [Fact]
    public async Task CloseAsync_Should_TearDown_When_SocketIsAlreadyFaulted()
    {
        // arrange
        using var socket = new TeardownWebSocket();
        var client = await SocketClient.ConnectAsync(
            socket,
            new SocketClientOptions(),
            TestContext.Current.CancellationToken);
        socket.Fault();

        // act
        await client.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(socket.IsDisposed);
    }

    private static OperationRequest CreateOperationRequest()
        => new(
            "{ value }"u8.ToArray(),
            null,
            null,
            null,
            VariableValues.Empty,
            JsonSegment.Empty);

    private sealed class UnusedArenaSource : IMemoryArenaSource
    {
        public IMemoryArena GetNextArena()
            => throw new InvalidOperationException("No result payload is expected.");
    }

    private sealed class TeardownWebSocket : WebSocket
    {
        private readonly Queue<byte[]> _receivedMessages = [];
        private readonly SemaphoreSlim _messageReceived = new(0);
        private readonly List<string> _events = [];
        private int _disposed;
        private int _state = (int)WebSocketState.Open;

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => (WebSocketState)Volatile.Read(ref _state);

        public override string SubProtocol => WellKnownProtocols.GraphQL_Transport_WS;

        public IReadOnlyList<string> Events
        {
            get
            {
                lock (_events)
                {
                    return _events.ToArray();
                }
            }
        }

        public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

        public override void Abort()
            => Volatile.Write(ref _state, (int)WebSocketState.Aborted);

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            AddEvent("close");
            Volatile.Write(ref _state, (int)WebSocketState.Closed);
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => CloseAsync(closeStatus, statusDescription, cancellationToken);

        public override void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            Volatile.Write(ref _state, (int)WebSocketState.Closed);
        }

        public void Fault()
            => Volatile.Write(ref _state, (int)WebSocketState.Aborted);

        public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken)
        {
            await _messageReceived.WaitAsync(cancellationToken);

            byte[] message;

            lock (_receivedMessages)
            {
                message = _receivedMessages.Dequeue();
            }

            message.CopyTo(buffer);
            return new ValueWebSocketReceiveResult(message.Length, WebSocketMessageType.Text, true);
        }

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            using var document = JsonDocument.Parse(buffer);
            var type = document.RootElement.GetProperty("type").GetString()!;
            AddEvent(type);

            if (type == "connection_init")
            {
                EnqueueReceivedMessage("""{"type":"connection_ack"}"""u8.ToArray());
            }

            return Task.CompletedTask;
        }

        private void AddEvent(string value)
        {
            lock (_events)
            {
                _events.Add(value);
            }
        }

        private void EnqueueReceivedMessage(byte[] message)
        {
            lock (_receivedMessages)
            {
                _receivedMessages.Enqueue(message);
            }

            _messageReceived.Release();
        }
    }
}
