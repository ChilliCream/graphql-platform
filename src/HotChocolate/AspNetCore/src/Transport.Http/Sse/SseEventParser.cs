#if FUSION
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Text.Json;

#else
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

internal static class SseEventParser
{
    private static ReadOnlySpan<byte> Event => "event:"u8;
    private static ReadOnlySpan<byte> Data => "data:"u8;
    private static ReadOnlySpan<byte> NextEvent => "next"u8;
    private static ReadOnlySpan<byte> CompleteEvent => "complete"u8;

#if FUSION
    public static SseEventData Parse(List<byte[]> message, int lastBufferSize)
    {
        Debug.Assert(message.Count > 0);
        Debug.Assert(message.All(c => c.Length >= JsonMemory.BufferSize));

        var position = 0;

        // if we only have a single chunk we must apply the lastBufferSize
        // to the first chunk as it's also the last chunk.
        var firstChunk = message.Count == 1 ? message[position].AsSpan(0, lastBufferSize) : message[position];

        // The event type will always fit in the first chunk, so we do not need to do any heavy lifting here.
        var type = ParseEventType(firstChunk, ref position);

        switch (type)
        {
            case SseEventType.Next:
                var (data, size, usedChunks) = ParseData(message, lastBufferSize, position);
                return new SseEventData(SseEventType.Next, data, size, usedChunks);

            case SseEventType.Complete:
                return new SseEventData(SseEventType.Complete, null, -1, -1);

            default:
                return new SseEventData(SseEventType.Unknown, null, -1, -1);
        }
    }
#else
    public static SseEventData Parse(ReadOnlySpan<byte> message)
    {
        var position = 0;
        var type = ParseEventType(message, ref position);

        switch (type)
        {
            case SseEventType.Next:
                var buffer = ParseData(message, position);
                return new SseEventData(SseEventType.Next, buffer);

            case SseEventType.Complete:
                return new SseEventData(SseEventType.Complete, null);

            default:
                return new SseEventData(SseEventType.Unknown, null);
        }
    }
#endif

#if FUSION
    /// <summary>
    /// Collects <c>data:</c> lines until the blank-line separator and concatenates them with LF.
    /// </summary>
    private static (byte[][] Chunks, int LastBufferSize, int UsedChunks) ParseData(
        List<byte[]> message,
        int lastBufferSize,
        int position)
    {
        var dataLength = message.Count == 1
            ? lastBufferSize - position
            : ((message.Count - 1) * JsonMemory.BufferSize) + lastBufferSize - position;

        if (dataLength < Data.Length || !ConsumeToken(message, lastBufferSize, ref position, Data))
        {
            throw new GraphQLHttpStreamException("Invalid GraphQL over SSE Message Format.");
        }

        var prependLineFeed = false;
        var nextWritePosition = 0;

        do
        {
            SkipWhitespaces(message, lastBufferSize, ref position);

            var lineStart = position;
            ParseLine(message, lastBufferSize, dataLength, lineStart, out var lineEnd, out position);

            if (lineEnd > position)
            {
                SkipWhitespaces(message, lastBufferSize, ref lineStart);

                // Make sure we don't skip past the end of the line
                if (lineStart > lineEnd)
                {
                    lineStart = lineEnd;
                }
            }

            ShiftDataLeft(message, lastBufferSize, ref nextWritePosition, lineStart, lineEnd, prependLineFeed);
            prependLineFeed = true;
        } while (position < dataLength);

        // Calculate how many chunks we actually used and the final chunk size
        var usedChunks = nextWritePosition == 0 ? 0 : ((nextWritePosition - 1) / JsonMemory.BufferSize) + 1;
        var newLastBufferSize = nextWritePosition == 0 ? 0 : ((nextWritePosition - 1) % JsonMemory.BufferSize) + 1;

        // Create new list with only the used chunks
        var result = ArrayPool<byte[]>.Shared.Rent(usedChunks);
        for (var i = 0; i < usedChunks; i++)
        {
            result[i] = message[i];
        }

        return (result, newLastBufferSize, usedChunks);
    }

    private static void ParseLine(
        List<byte[]> chunks,
        int lastBufferSize,
        int dataLength,
        int start,
        out int end,
        out int next)
    {
        var chunkIndex = start / JsonMemory.BufferSize;
        var localPosition = start % JsonMemory.BufferSize;
        var bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;
        var chunk = chunks[chunkIndex].AsSpan(localPosition, bufferSize - localPosition);

        // we first try to find the line feed character in the current chunk.
        if (FindLineEnd(chunk, out end, out next))
        {
            return;
        }

        // we already checked if the linefeed is in the current chunk so we can start with next one.
        while (++chunkIndex < chunks.Count)
        {
            bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;
            chunk = chunks[chunkIndex].AsSpan(0, bufferSize);
            localPosition = 0;

            if (FindLineEnd(chunk, out end, out next))
            {
                return;
            }
        }

        end = dataLength;
        next = dataLength + 1;
        return;

        bool FindLineEnd(Span<byte> chunk, out int end, out int nextPosition)
        {
            var lineFeedIndex = chunk.IndexOf((byte)'\n');

            if (lineFeedIndex != -1)
            {
                var possibleEnd = (chunkIndex * JsonMemory.BufferSize) + localPosition + lineFeedIndex;

                // let's check if we have a CRLF sequence
                if (lineFeedIndex > 0 && chunk[lineFeedIndex - 1] == (byte)'\r')
                {
                    nextPosition = possibleEnd + 1;
                    end = possibleEnd - 1;
                    return true;
                }

                nextPosition = possibleEnd + 1;
                end = possibleEnd;
                return true;
            }

            end = -1;
            nextPosition = -1;
            return false;
        }
    }

    private static void ShiftDataLeft(
        List<byte[]> chunks,
        int lastBufferSize,
        ref int nextWritePosition,
        int startData,
        int endData,
        bool prependLineFeed)
    {
        var destinationChunkIndex = nextWritePosition / JsonMemory.BufferSize;
        var destinationLocalPosition = nextWritePosition % JsonMemory.BufferSize;

        // First, handle the optional line feed
        if (prependLineFeed)
        {
            chunks[destinationChunkIndex][destinationLocalPosition] = (byte)'\n';
            nextWritePosition++;
        }

        // Calculate how much data we need to copy
        var dataLength = endData - startData;
        if (dataLength <= 0)
        {
            return;
        }

        var sourcePosition = startData;
        var destinationPosition = nextWritePosition;

        // Fast path: check if both source and destination fit in single chunks
        var sourceChunkIndex = sourcePosition / JsonMemory.BufferSize;
        var sourceLocalPosition = sourcePosition % JsonMemory.BufferSize;
        var sourceBufferSize = sourceChunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

        destinationChunkIndex = destinationPosition / JsonMemory.BufferSize;
        destinationLocalPosition = destinationPosition % JsonMemory.BufferSize;
        var destinationBufferSize = destinationChunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

        // if we can read the data from a single chunk and write the data to a single chunk
        // we will go for a single copy.
        if (sourceLocalPosition + dataLength <= sourceBufferSize
            && destinationLocalPosition + dataLength <= destinationBufferSize)
        {
            var source = chunks[sourceChunkIndex].AsSpan(sourceLocalPosition, dataLength);
            var destination = chunks[destinationChunkIndex].AsSpan(destinationLocalPosition, dataLength);

            source.CopyTo(destination);

            nextWritePosition += dataLength;
            return;
        }

        // however, the data might be distributed across chunks so we still go for an optimized copy
        // but might end up with multiple copies.
        while (dataLength > 0)
        {
            sourceChunkIndex = sourcePosition / JsonMemory.BufferSize;
            sourceLocalPosition = sourcePosition % JsonMemory.BufferSize;
            sourceBufferSize = sourceChunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

            destinationChunkIndex = destinationPosition / JsonMemory.BufferSize;
            destinationLocalPosition = destinationPosition % JsonMemory.BufferSize;
            destinationBufferSize = destinationChunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

            // How much data can we read from the current source chunk?
            var sourceSize = sourceBufferSize - sourceLocalPosition;

            // How much can we write to the current destination chunk?
            var destinationSize = destinationBufferSize - destinationLocalPosition;

            // Copy the minimum of what we can read and what we can write.
            var bytesToCopy = Math.Min(sourceSize, destinationSize);

            var source = chunks[sourceChunkIndex].AsSpan(sourceLocalPosition, bytesToCopy);
            var destination = chunks[destinationChunkIndex].AsSpan(destinationLocalPosition, bytesToCopy);

            source.CopyTo(destination);

            sourcePosition += bytesToCopy;
            destinationPosition += bytesToCopy;
            dataLength -= bytesToCopy;
        }

        nextWritePosition += (endData - startData);
    }
#else
    /// <summary>
    /// Collects <c>data:</c> lines until the blank-line separator and concatenates them with LF.
    /// </summary>
    private static PooledArrayWriter ParseData(ReadOnlySpan<byte> span, int position)
    {
        if (span.Length - position < Data.Length || !span[position..].StartsWith(Data))
        {
            throw new GraphQLHttpStreamException("Invalid GraphQL over SSE Message Format.");
        }

        var payload = new PooledArrayWriter();

        try
        {
            while (ConsumeToken(span, ref position, Data))
            {
                SkipWhitespaces(span, ref position);

                // read one logical line up to LF or end
                var remaining = span[position..];
                var lineBreak = remaining.IndexOf((byte)'\n');
                ReadOnlySpan<byte> line;
                switch (lineBreak)
                {
                    case -1:
                        line = remaining;
                        position = span.Length;
                        break;
                    case > 0 when remaining[lineBreak - 1] == (byte)'\r':
                        line = remaining[..(lineBreak - 1)];
                        position += lineBreak + 1;
                        break;
                    default:
                        line = remaining[..lineBreak];
                        position += lineBreak + 1;
                        break;
                }

                // append to buffer (insert LF between lines)
                if (payload.Length > 0)
                {
                    payload.GetSpan(1)[0] = (byte)'\n';
                    payload.Advance(1);
                }

                if (line.Length > 0)
                {
                    line.CopyTo(payload.GetSpan(line.Length));
                    payload.Advance(line.Length);
                }
            }

            return payload;
        }
        catch
        {
            payload.Dispose();
            throw;
        }
    }
#endif

    private static SseEventType ParseEventType(ReadOnlySpan<byte> data, ref int position)
    {
        if (ExpectEvent(data, ref position))
        {
            if (ExpectNext(data, ref position))
            {
                return SseEventType.Next;
            }

            if (ExpectComplete(data, ref position))
            {
                return SseEventType.Complete;
            }
        }

        return SseEventType.Unknown;
    }

    private static bool ExpectEvent(ReadOnlySpan<byte> data, ref int position)
        => ConsumeToken(data, ref position, Event);

    private static bool ExpectNext(ReadOnlySpan<byte> data, ref int position)
        => ConsumeTokenWithOptionalWhitespace(data, ref position, NextEvent);

    private static bool ExpectComplete(ReadOnlySpan<byte> data, ref int position)
        => ConsumeTokenWithOptionalWhitespace(data, ref position, CompleteEvent);

    private static bool ConsumeTokenWithOptionalWhitespace(
        ReadOnlySpan<byte> data,
        ref int position,
        ReadOnlySpan<byte> token)
    {
        SkipWhitespaces(data, ref position);

        if (ConsumeToken(data, ref position, token))
        {
            SkipNewLine(data, ref position);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ConsumeToken(ReadOnlySpan<byte> data, ref int position, ReadOnlySpan<byte> token)
    {
        if (data.Length < token.Length + position || !data[position..].StartsWith(token))
        {
            return false;
        }

        position += token.Length;
        return true;
    }

#if FUSION
    private static bool ConsumeToken(
        List<byte[]> chunks,
        int lastBufferSize,
        ref int position,
        ReadOnlySpan<byte> token)
    {
        var startPosition = position;

        var chunkIndex = position / JsonMemory.BufferSize;
        var localPosition = position % JsonMemory.BufferSize;
        var bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

        // if the token fits into a single chunk we take the fast path and consume the token in one go.
        if (localPosition + token.Length <= bufferSize)
        {
            if (ConsumeToken(chunks[chunkIndex], ref localPosition, token))
            {
                position = (chunkIndex * JsonMemory.BufferSize) + localPosition;
                return true;
            }

            return false;
        }

        // if however it spans multiple chunks we need to parse token character by token character.
        for (var i = 0; i < token.Length; i++)
        {
            chunkIndex = position / JsonMemory.BufferSize;
            localPosition = position % JsonMemory.BufferSize;
            bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;

            if (chunkIndex >= chunks.Count || localPosition >= bufferSize)
            {
                position = startPosition;
                return false;
            }

            if (chunks[chunkIndex][localPosition] != token[i])
            {
                position = startPosition;
                return false;
            }

            position++;
        }

        return true;
    }
#endif

    private static void SkipWhitespaces(ReadOnlySpan<byte> data, ref int position)
    {
        while (data.Length > position && IsWhitespace(data[position]))
        {
            position++;
        }
    }

#if FUSION
    private static void SkipWhitespaces(List<byte[]> chunks, int lastBufferSize, ref int position)
    {
        var chunkIndex = position / JsonMemory.BufferSize;
        var localPosition = position % JsonMemory.BufferSize;

        if (chunkIndex >= chunks.Count)
        {
            return;
        }

        var bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;
        var chunk = chunks[chunkIndex].AsSpan();

        // We try to move past white spaces in the current chunk.
        while (localPosition < bufferSize && IsWhitespace(chunk[localPosition]))
        {
            localPosition++;
        }

        position = (chunkIndex * JsonMemory.BufferSize) + localPosition;

        // If we hit end of chunk but not end of data, we continue with the slow path
        if (localPosition >= bufferSize && chunkIndex + 1 < chunks.Count)
        {
            // Continue to next chunks
            chunkIndex++;

            while (chunkIndex < chunks.Count)
            {
                bufferSize = chunkIndex + 1 == chunks.Count ? lastBufferSize : JsonMemory.BufferSize;
                chunk = chunks[chunkIndex];
                localPosition = 0;

                // Process entire current chunk
                while (localPosition < bufferSize && IsWhitespace(chunk[localPosition]))
                {
                    localPosition++;
                }

                // Update global position
                position = (chunkIndex * JsonMemory.BufferSize) + localPosition;

                // If we found non-whitespace, we're done
                if (localPosition < bufferSize)
                {
                    break;
                }

                // Move to next chunk
                chunkIndex++;
            }
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespace(byte b) => b is (byte)' ' or (byte)'\t';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SkipNewLine(ReadOnlySpan<byte> data, ref int position)
    {
        if (data.Length > position && data[position] == (byte)'\r')
        {
            position++;
        }

        if (data.Length > position && data[position] == (byte)'\n')
        {
            position++;
        }
    }
}
