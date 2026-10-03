using System.Buffers;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket;

internal static class WebSocketMessageParser
{
    public static WebSocketMessageLocation Locate(ReadOnlySequence<byte> message)
    {
        var reader = new Utf8JsonReader(message);
        var type = SocketMessageType.None;
        string? id = null;
        var payload = default(ReadOnlySequence<byte>);
        var hasPayload = false;

        while (reader.Read())
        {
            if (reader.CurrentDepth != 1 || reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            if (reader.ValueTextEquals(Utf8MessageProperties.TypeProp))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                {
                    continue;
                }

                type = ParseMessageType(ref reader);
            }
            else if (reader.ValueTextEquals(Utf8MessageProperties.IdProp))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.String)
                {
                    id = reader.GetString();
                }
            }
            else if (reader.ValueTextEquals(Utf8MessageProperties.PayloadProp))
            {
                if (!reader.Read())
                {
                    continue;
                }

                var start = reader.TokenStartIndex;
                reader.Skip();
                payload = message.Slice(start, reader.BytesConsumed - start);
                hasPayload = true;
            }
        }

        return new WebSocketMessageLocation(type, id, payload, hasPayload);
    }

    public static PooledSocketPayload CopyPayload(
        WebSocketMessageLocation location,
        ArrayPool<byte> pool,
        ReadOnlySpan<byte> prefix = default,
        ReadOnlySpan<byte> suffix = default)
    {
        if (!location.HasPayload)
        {
            throw ThrowHelper.MessageHasNoPayload();
        }

        var length = checked(prefix.Length + (int)location.Payload.Length + suffix.Length);
        var buffer = pool.Rent(length);

        try
        {
            var destination = buffer.AsSpan(0, length);
            prefix.CopyTo(destination);
            location.Payload.CopyTo(destination[prefix.Length..]);
            suffix.CopyTo(destination[(prefix.Length + (int)location.Payload.Length)..]);
            return new PooledSocketPayload(pool, buffer, length);
        }
        catch
        {
            pool.Return(buffer);
            throw;
        }
    }

    public static SourceResultDocument ParsePayload(
        PooledSocketPayload payload,
        IMemoryArena arena)
    {
        var segments = arena.RentSegmentTable(1);
        var remaining = payload.Length;
        var offset = 0;
        var chunkIndex = 0;

        while (remaining > 0)
        {
            if (chunkIndex >= SourceResultDocument.DataMaxChunks)
            {
                throw ThrowHelper.SourceResultDocumentCapacityExceeded();
            }

            if (chunkIndex >= segments.Length)
            {
                arena.GrowSegmentTable(ref segments);
            }

            var length = Math.Min(SourceResultDocument.GetDataChunkSize(chunkIndex), remaining);
            segments[chunkIndex] = new MemorySegment(payload.Buffer, offset, length);
            remaining -= length;
            offset += length;
            chunkIndex++;
        }

        if (chunkIndex == 0)
        {
            throw ThrowHelper.MessageHasNoPayload();
        }

        return SourceResultDocument.ParseFilled(
            arena,
            segments,
            chunkIndex,
            segments[chunkIndex - 1].Length);
    }

    private static SocketMessageType ParseMessageType(ref Utf8JsonReader reader)
    {
        if (reader.ValueTextEquals(Utf8Messages.Ping))
        {
            return SocketMessageType.Ping;
        }

        if (reader.ValueTextEquals(Utf8Messages.Pong))
        {
            return SocketMessageType.Pong;
        }

        if (reader.ValueTextEquals(Utf8Messages.Next))
        {
            return SocketMessageType.Next;
        }

        if (reader.ValueTextEquals(Utf8Messages.Error))
        {
            return SocketMessageType.Error;
        }

        if (reader.ValueTextEquals(Utf8Messages.Complete))
        {
            return SocketMessageType.Complete;
        }

        if (reader.ValueTextEquals(Utf8Messages.ConnectionAccept))
        {
            return SocketMessageType.ConnectionAccept;
        }

        return SocketMessageType.None;
    }
}

internal readonly record struct WebSocketMessageLocation(
    SocketMessageType Type,
    string? Id,
    ReadOnlySequence<byte> Payload,
    bool HasPayload);

internal enum SocketMessageType
{
    None,
    Ping,
    Pong,
    Next,
    Error,
    Complete,
    ConnectionAccept
}

internal sealed class PooledSocketPayload(
    ArrayPool<byte> pool,
    byte[] buffer,
    int length) : IDisposable
{
    private ArrayPool<byte>? _pool = pool;
    private byte[]? _buffer = buffer;

    public byte[] Buffer
        => _buffer ?? throw ThrowHelper.PayloadBufferDisposed();

    public int Length { get; } = length;

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        var currentPool = Interlocked.Exchange(ref _pool, null);

        if (buffer is not null)
        {
            currentPool!.Return(buffer);
        }
    }
}
