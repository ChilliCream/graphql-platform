using System.Buffers;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using JsonDocument = System.Text.Json.JsonDocument;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport;
using HotChocolate.Fusion.Transport.Sockets.Client;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Fusion.Transport.Sockets;

public class FusionSocketReaderTests
{
    [Fact]
    public void Locate_Should_FindTopLevelValues_When_PayloadContainsProtocolPropertyNames()
    {
        // arrange
        var message = CreateSequence(
            """
            {"payload":{"data":{"id":"nested","type":"error","payload":"nested"}},"id":"operation-1","type":"next"}
            """u8.ToArray(),
            7);

        // act
        var location = WebSocketMessageParser.Locate(message);

        // assert
        Assert.Equal(SocketMessageType.Next, location.Type);
        Assert.Equal("operation-1", location.Id);
        Assert.Equal(
            "{\"data\":{\"id\":\"nested\",\"type\":\"error\",\"payload\":\"nested\"}}",
            Encoding.UTF8.GetString(location.Payload.ToArray()));
    }

    [Fact]
    public void ErrorMessage_Should_WrapErrors_When_CopyingPayload()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var location = WebSocketMessageParser.Locate(
            new ReadOnlySequence<byte>(
                """{"type":"error","id":"operation-1","payload":[{"message":"failure"}]}"""u8.ToArray()));
        using var arena = new MemoryArena();
        var message = ErrorMessage.From(location, pool);

        // act
        message.ParsePayload(new FixedArenaSource(arena));
        var document = message.TakePayload();

        // assert
        Assert.Equal(
            "failure",
            document.Root.GetProperty("errors")[0].GetProperty("message").GetString());
        Assert.Equal(0, pool.ReturnCount);

        document.Dispose();
        message.Dispose();
        arena.Seal();
        Assert.Equal(1, pool.ReturnCount);
    }

    [Fact]
    public async Task OnNext_Should_SerializeOverflowCompleteBehindSiblingSend()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var options = new SocketClientOptions
        {
            MaxOperationQueueBytes = 20,
            PayloadBufferPool = pool
        };
        using var socket = new RacingWebSocket();
        var context = new SocketClientContext(socket, options);
        var handler = new GraphQLOverWebSocketProtocolHandler();
        var overflowArenaSource = new SubscriptionArenaSource();
        var siblingArenaSource = new SubscriptionArenaSource();
        await using var overflowResult = await handler.ExecuteAsync(
            context,
            CreateOperationRequest(),
            overflowArenaSource,
            deferPayloadParsing: true,
            TestContext.Current.CancellationToken);
        var overflowId = GetMessageIds(socket.SentMessages, "subscribe").Single();
        socket.HoldNextSend();
        var siblingResultTask = handler.ExecuteAsync(
            context,
            CreateOperationRequest(),
            siblingArenaSource,
            deferPayloadParsing: true,
            TestContext.Current.CancellationToken).AsTask();
        await socket.HeldSendEntered.WaitAsync(TestContext.Current.CancellationToken);

        // act
        await handler.OnReceiveAsync(
            context,
            CreateNextFrame(overflowId, 1),
            TestContext.Current.CancellationToken);
        await handler.OnReceiveAsync(
            context,
            CreateNextFrame(overflowId, 2),
            TestContext.Current.CancellationToken);
        var error = await Assert.ThrowsAsync<SocketOperationException>(
            async () => await ReadFirstResultAsync(overflowResult));
        socket.ReleaseHeldSend();
        await using var siblingResult = await siblingResultTask;
        await socket.WaitForSendCountAsync(3, TestContext.Current.CancellationToken);
        var siblingId = GetMessageIds(socket.SentMessages, "subscribe")
            .Single(id => !string.Equals(id, overflowId, StringComparison.Ordinal));
        await handler.OnReceiveAsync(
            context,
            CreateNextFrame(siblingId, 3),
            TestContext.Current.CancellationToken);
        await using var siblingEnumerator = siblingResult
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        if (!await siblingEnumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("The sibling operation completed without a result.");
        }

        var siblingDocument = siblingEnumerator.Current;

        // assert
        Assert.Contains("20 bytes", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, socket.MaxConcurrentSends);
        Assert.Equal(0, socket.AbortCount);
        Assert.Equal([overflowId], GetMessageIds(socket.SentMessages, "complete"));
        Assert.Equal(3, siblingDocument.Root.GetProperty("data").GetProperty("value").GetInt32());

        siblingDocument.Dispose();
        ((MemoryArena)siblingArenaSource.Arena).Seal();
        await handler.OnReceiveAsync(
            context,
            CreateCompleteFrame(siblingId),
            TestContext.Current.CancellationToken);

        if (await siblingEnumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("The sibling operation continued after completion.");
        }
    }

    [Fact]
    public async Task OnReceiveAsync_Should_ReturnBuffer_When_FrameHasNoObserver()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var options = new SocketClientOptions { PayloadBufferPool = pool };
        using var socket = new StubWebSocket();
        var context = new SocketClientContext(socket, options);
        var handler = new GraphQLOverWebSocketProtocolHandler();
        var message = new ReadOnlySequence<byte>(
            """{"type":"next","id":"unclaimed","payload":{"data":{"value":1}}}"""u8.ToArray());

        // act
        await handler.OnReceiveAsync(
            context,
            message,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, pool.RentCount);
        Assert.Equal(1, pool.ReturnCount);
    }

    [Fact]
    public async Task OnNext_Should_ParseAtReceive_When_OperationIsEager()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var arenaSource = new RecordingArenaSource();
        using var observer = new DataMessageObserver(
            "operation-1",
            arenaSource,
            deferPayloadParsing: false,
            maxQueueBytes: 1024,
            new RecordingCompletion());

        // act
        observer.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":1}}", pool));
        var arenasAfterReceive = arenaSource.Arenas.Count;
        var message = (FusionDataMessage)(await observer.TryReadNextAsync(
            TestContext.Current.CancellationToken))!;
        var document = message.TakePayload();

        // assert
        Assert.Equal(1, arenasAfterReceive);
        Assert.Single(arenaSource.Arenas);
        Assert.Equal(1, document.Root.GetProperty("data").GetProperty("value").GetInt32());

        document.Dispose();
        message.Dispose();
        arenaSource.Dispose();
    }

    [Fact]
    public async Task ReadResultsAsync_Should_ReleasePriorPayloadBeforeDeliveringNextDeferredEvent()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var arenaSource = new SubscriptionArenaSource();
        var completion = new RecordingCompletion();
        using var observer = new DataMessageObserver(
            "operation-1",
            arenaSource,
            deferPayloadParsing: true,
            maxQueueBytes: 1024,
            completion);
        observer.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":1}}", pool));
        observer.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":2}}", pool));
        await using var result = new SocketResult(
            observer,
            new StubSubscription(),
            completion,
            default);
        await using var enumerator = result
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        if (!await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("The first deferred event was not delivered.");
        }

        var firstDocument = enumerator.Current;
        var firstValue = firstDocument.Root.GetProperty("data").GetProperty("value").GetInt32();
        var firstArena = arenaSource.Arena;
        firstDocument.Dispose();
        ((MemoryArena)firstArena).Seal();

        if (!await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("The second deferred event was not delivered.");
        }

        var secondDocument = enumerator.Current;
        var secondValue = secondDocument.Root.GetProperty("data").GetProperty("value").GetInt32();

        // assert
        Assert.Equal(1, firstValue);
        Assert.Equal(2, secondValue);
        Assert.NotSame(firstArena, arenaSource.Arena);
        Assert.Equal(1, pool.ReturnCount);

        secondDocument.Dispose();
        ((MemoryArena)arenaSource.Arena).Seal();
    }

    private static OperationRequest CreateOperationRequest()
        => new(
            "{ value }"u8.ToArray(),
            null,
            null,
            null,
            VariableValues.Empty,
            JsonSegment.Empty);

    private static ReadOnlySequence<byte> CreateNextFrame(string id, int value)
        => new(
            Encoding.UTF8.GetBytes(
                $"{{\"type\":\"next\",\"id\":\"{id}\",\"payload\":{{\"data\":{{\"value\":{value}}}}}}}"));

    private static ReadOnlySequence<byte> CreateCompleteFrame(string id)
        => new(Encoding.UTF8.GetBytes($"{{\"type\":\"complete\",\"id\":\"{id}\"}}"));

    private static string[] GetMessageIds(
        IReadOnlyList<string> messages,
        string type)
    {
        var ids = new List<string>();

        foreach (var message in messages)
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            if (root.GetProperty("type").GetString() == type)
            {
                ids.Add(root.GetProperty("id").GetString()!);
            }
        }

        return ids.ToArray();
    }

    private static async Task ReadFirstResultAsync(SocketResult result)
    {
        await using var enumerator = result
            .ReadResultsAsync()
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();
    }

    private static NextMessage CreateNextMessage(
        string id,
        string payload,
        ArrayPool<byte> pool)
    {
        var frame = Encoding.UTF8.GetBytes(
            $"{{\"type\":\"next\",\"id\":\"{id}\",\"payload\":{payload}}}");
        var location = WebSocketMessageParser.Locate(new ReadOnlySequence<byte>(frame));
        return NextMessage.From(location, pool);
    }

    private static ReadOnlySequence<byte> CreateSequence(byte[] data, int segmentLength)
    {
        BufferSegment? first = null;
        BufferSegment? last = null;

        for (var offset = 0; offset < data.Length; offset += segmentLength)
        {
            var length = Math.Min(segmentLength, data.Length - offset);
            var segment = new BufferSegment(data.AsMemory(offset, length));

            if (first is null)
            {
                first = segment;
            }
            else
            {
                last!.Append(segment);
            }

            last = segment;
        }

        return new ReadOnlySequence<byte>(first!, 0, last!, last!.Memory.Length);
    }

    private sealed class FixedArenaSource(IMemoryArena arena) : IMemoryArenaSource
    {
        public IMemoryArena GetNextArena() => arena;
    }

    private sealed class RecordingArenaSource : IMemoryArenaSource, IDisposable
    {
        public List<MemoryArena> Arenas { get; } = [];

        public IMemoryArena GetNextArena()
        {
            var arena = new MemoryArena();
            Arenas.Add(arena);
            return arena;
        }

        public void Dispose()
        {
            foreach (var arena in Arenas)
            {
                arena.Seal();
                arena.Dispose();
            }
        }
    }

    private sealed class RecordingCompletion : IDataCompletion
    {
        public int SendCount { get; private set; }

        public void MarkDataStreamCompleted()
        {
        }

        public void TrySendCompleteMessage()
            => SendCount++;

        public ValueTask TrySendCompleteMessageAsync()
        {
            SendCount++;
            return default;
        }
    }

    private sealed class StubSubscription : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        public int RentCount { get; private set; }

        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            RentCount++;
            return new byte[minimumLength];
        }

        public override void Return(byte[] array, bool clearArray = false)
            => ReturnCount++;
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public void Append(BufferSegment next)
        {
            next.RunningIndex = RunningIndex + Memory.Length;
            Next = next;
        }
    }

    private sealed class StubWebSocket : WebSocket
    {
        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => WebSocketState.Open;

        public override string SubProtocol => WellKnownProtocols.GraphQL_Transport_WS;

        public override void Abort()
        {
        }

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Dispose()
        {
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
            => Task.CompletedTask;
    }

    private sealed class RacingWebSocket : WebSocket
    {
        private readonly ConcurrentQueue<string> _sentMessages = new();
        private readonly SemaphoreSlim _messageSent = new(0);
        private TaskCompletionSource _heldSendEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private TaskCompletionSource _releaseHeldSend = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private int _abortCount;
        private int _activeSends;
        private int _holdNextSend;
        private int _maxConcurrentSends;

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string? CloseStatusDescription => null;

        public override WebSocketState State => WebSocketState.Open;

        public override string SubProtocol => WellKnownProtocols.GraphQL_Transport_WS;

        public int AbortCount => Volatile.Read(ref _abortCount);

        public Task HeldSendEntered => _heldSendEntered.Task;

        public int MaxConcurrentSends => Volatile.Read(ref _maxConcurrentSends);

        public IReadOnlyList<string> SentMessages => _sentMessages.ToArray();

        public override void Abort()
            => Interlocked.Increment(ref _abortCount);

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override void Dispose()
        {
            _messageSent.Dispose();
        }

        public void HoldNextSend()
        {
            _heldSendEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseHeldSend = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref _holdNextSend, 1);
        }

        public void ReleaseHeldSend()
            => _releaseHeldSend.TrySetResult();

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public override async Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            var activeSends = Interlocked.Increment(ref _activeSends);
            UpdateMaxConcurrentSends(activeSends);

            try
            {
                if (activeSends > 1)
                {
                    throw new InvalidOperationException("Concurrent WebSocket sends are not supported.");
                }

                if (Interlocked.Exchange(ref _holdNextSend, 0) == 1)
                {
                    _heldSendEntered.TrySetResult();
                    await _releaseHeldSend.Task.WaitAsync(cancellationToken);
                }

                _sentMessages.Enqueue(Encoding.UTF8.GetString(buffer));
                _messageSent.Release();
            }
            finally
            {
                Interlocked.Decrement(ref _activeSends);
            }
        }

        public async Task WaitForSendCountAsync(int count, CancellationToken cancellationToken)
        {
            while (_sentMessages.Count < count)
            {
                await _messageSent.WaitAsync(cancellationToken);
            }
        }

        private void UpdateMaxConcurrentSends(int activeSends)
        {
            var maxConcurrentSends = Volatile.Read(ref _maxConcurrentSends);

            while (activeSends > maxConcurrentSends)
            {
                var observed = Interlocked.CompareExchange(
                    ref _maxConcurrentSends,
                    activeSends,
                    maxConcurrentSends);

                if (observed == maxConcurrentSends)
                {
                    return;
                }

                maxConcurrentSends = observed;
            }
        }
    }
}
