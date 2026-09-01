using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
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
    public async Task OnNext_Should_SendCompleteAndFailOperation_When_ByteLimitIsExceeded()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var completion = new RecordingCompletion();
        using var arena = new MemoryArena();
        using var siblingArena = new MemoryArena();
        using var observer = new DataMessageObserver(
            "operation-1",
            new FixedArenaSource(arena),
            deferPayloadParsing: true,
            maxQueueBytes: 20,
            completion);
        using var siblingObserver = new DataMessageObserver(
            "operation-2",
            new FixedArenaSource(siblingArena),
            deferPayloadParsing: true,
            maxQueueBytes: 1024,
            new RecordingCompletion());
        var stream = new MessageStream();
        using var subscription = stream.Subscribe(observer);
        using var siblingSubscription = stream.Subscribe(siblingObserver);

        // act
        stream.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":1}}", pool));
        stream.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":2}}", pool));
        stream.OnNext(CreateNextMessage("operation-2", "{\"data\":{\"value\":3}}", pool));
        var error = await Assert.ThrowsAsync<SocketOperationException>(
            async () => await observer.TryReadNextAsync(TestContext.Current.CancellationToken));
        var sibling = (FusionDataMessage)(await siblingObserver.TryReadNextAsync(
            TestContext.Current.CancellationToken))!;
        var siblingDocument = sibling.TakePayload();

        // assert
        Assert.Contains("20 bytes", error.Message, StringComparison.Ordinal);
        Assert.Equal(1, completion.SendCount);
        Assert.Equal(3, siblingDocument.Root.GetProperty("data").GetProperty("value").GetInt32());

        siblingDocument.Dispose();
        sibling.Dispose();
        arena.Seal();
        siblingArena.Seal();
        Assert.Equal(3, pool.ReturnCount);
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
    public async Task TryReadNextAsync_Should_UseFreshArena_When_ParsingDeferredEvents()
    {
        // arrange
        var pool = new TrackingArrayPool();
        var arenaSource = new RecordingArenaSource();
        var completion = new RecordingCompletion();
        using var observer = new DataMessageObserver(
            "operation-1",
            arenaSource,
            deferPayloadParsing: true,
            maxQueueBytes: 1024,
            completion);
        observer.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":1}}", pool));
        observer.OnNext(CreateNextMessage("operation-1", "{\"data\":{\"value\":2}}", pool));

        // act
        var arenasBeforeDequeue = arenaSource.Arenas.Count;
        var first = (FusionDataMessage)(await observer.TryReadNextAsync(
            TestContext.Current.CancellationToken))!;
        var firstDocument = first.TakePayload();
        var second = (FusionDataMessage)(await observer.TryReadNextAsync(
            TestContext.Current.CancellationToken))!;
        var secondDocument = second.TakePayload();

        // assert
        Assert.Equal(0, arenasBeforeDequeue);
        Assert.Equal(1, firstDocument.Root.GetProperty("data").GetProperty("value").GetInt32());
        Assert.Equal(2, secondDocument.Root.GetProperty("data").GetProperty("value").GetInt32());
        Assert.Collection(
            arenaSource.Arenas,
            firstArena => Assert.NotSame(firstArena, arenaSource.Arenas[1]),
            _ => { });

        firstDocument.Dispose();
        first.Dispose();
        secondDocument.Dispose();
        second.Dispose();
        arenaSource.Dispose();
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
}
