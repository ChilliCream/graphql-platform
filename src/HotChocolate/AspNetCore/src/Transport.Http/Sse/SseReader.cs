#if FUSION
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Text.Json;
#else
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

#if FUSION
internal class SseReader(HttpResponseMessage message) : IAsyncEnumerable<SourceResultDocument>
#else
internal class SseReader(HttpResponseMessage message) : IAsyncEnumerable<OperationResult>
#endif
{
    private static readonly StreamPipeReaderOptions s_options = new(
        pool: MemoryPool<byte>.Shared,
        bufferSize: 4096,
        minimumReadSize: 1,
        leaveOpen: true,
        useZeroByteReads: true);

#if FUSION
    public async IAsyncEnumerator<SourceResultDocument> GetAsyncEnumerator(
#else
    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
#endif
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await using var stream = await message.Content.ReadAsStreamAsync(cts.Token);
        var reader = PipeReader.Create(stream, s_options);
#if FUSION
        var eventBuffers = new List<byte[]>();
        var currentPosition = 0;
#else
        using var eventBuffer = new PooledArrayWriter();
#endif

        try
        {
            ReadResult result;
            do
            {
                result = await reader.ReadAsync(cts.Token).ConfigureAwait(false);
                if (result.IsCanceled)
                {
                    yield break;
                }

                var buffer = result.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;

                do
                {
                    var position = buffer.PositionOf((byte)'\n');
                    if (position is null)
                    {
                        // Mark what we've examined but not consumed
                        examined = buffer.End;
                        break;
                    }
#if FUSION
                    WriteLineToMessage(eventBuffers, ref currentPosition, buffer.Slice(0, position.Value));
#else
                    WriteLineToMessage(eventBuffer, buffer.Slice(0, position.Value));
#endif

#if FUSION
                    if (IsMessageComplete(eventBuffers, currentPosition))
#else
                    if (IsMessageComplete(eventBuffer.WrittenSpan))
#endif
                    {
#if FUSION
                        if (IsKeepAlive(eventBuffers, currentPosition))
                        {
                            currentPosition = 0;
                        }
#else
                        if (IsKeepAlive(eventBuffer.WrittenSpan))
                        {
                            eventBuffer.Reset();
                        }
#endif
                        else
                        {
#if FUSION
                            var eventMessage = SseEventParser.Parse(eventBuffers, currentPosition);
#else
                            var eventMessage = SseEventParser.Parse(eventBuffer.WrittenSpan);
#endif

                            switch (eventMessage.Type)
                            {
                                case SseEventType.Complete:
                                    reader.AdvanceTo(buffer.GetPosition(1, position.Value));
#if FUSION
                                    eventBuffers.Clear();
                                    JsonMemory.Return(eventBuffers);
#endif
                                    yield break;

                                case SseEventType.Next when eventMessage.Data is not null:
#if FUSION
                                    var leftOver = eventBuffers.Count - eventMessage.UsedChunks;
                                    currentPosition = 0;

                                    if (leftOver == 0)
                                    {
                                        eventBuffers.Clear();
                                    }
                                    else
                                    {
                                        eventBuffers.RemoveRange(0, eventBuffers.Count - leftOver);
                                    }

                                    yield return SourceResultDocument.Parse(
                                        eventMessage.Data,
                                        eventMessage.LastChunkSize,
                                        eventMessage.UsedChunks,
                                        pooledMemory: true);
#else
                                    eventBuffer.Reset();
                                    var document = JsonDocument.Parse(eventMessage.Data.WrittenMemory);
                                    var documentOwner = new JsonDocumentOwner(document, eventMessage.Data);
                                    yield return OperationResult.Parse(documentOwner);
#endif
                                    break;

                                default:
                                    throw new GraphQLHttpStreamException("Malformed message received.");
                            }
                        }
                    }

                    // Move past the processed line
                    var nextPosition = buffer.GetPosition(1, position.Value);
                    consumed = nextPosition;
                    buffer = buffer.Slice(nextPosition);
                } while (!buffer.IsEmpty);

                // Tell the reader how much we've consumed and examined
                reader.AdvanceTo(consumed, examined);
            } while (!result.IsCompleted);
        }
        finally
        {
            await cts.CancelAsync().ConfigureAwait(false);
            await reader.CompleteAsync().ConfigureAwait(false);
#if FUSION
            // we return whatever is in here.
            JsonMemory.Return(eventBuffers);
#endif
        }
    }

#if FUSION
    private static void WriteLineToMessage(List<byte[]> chunks, ref int currentPosition, ReadOnlySequence<byte> buffer)
    {
#else
    private static void WriteLineToMessage(PooledArrayWriter message, ReadOnlySequence<byte> buffer)
    {
        message.EnsureBufferCapacity((int)buffer.Length);
#endif

        if (buffer.IsSingleSegment)
        {
            var span = buffer.First.Span;

            // normalize line breaks.
            if (span.Length > 0 && span[^1] == (byte)'\r')
            {
                span = span[..^1];
            }
#if FUSION
            WriteBytesToChunks(chunks, ref currentPosition, span);
#else
            span.CopyTo(message.GetSpan(span.Length));
            message.Advance(span.Length);
#endif
        }
        else
        {
            var position = buffer.Start;
            while (buffer.TryGet(ref position, out var memory))
            {
                var span = memory.Span;

                // normalize line breaks.
                if (position.Equals(buffer.End) && span.Length > 0 && span[^1] == (byte)'\r')
                {
                    span = span[..^1];
                }
#if FUSION
                WriteBytesToChunks(chunks, ref currentPosition, span);
#else
                span.CopyTo(message.GetSpan(span.Length));
                message.Advance(span.Length);
#endif
            }
        }

        // re-add unified line break (LF only)
#if FUSION
        WriteBytesToChunks(chunks, ref currentPosition, [(byte)'\n']);
#else
        message.GetSpan(1)[0] = (byte)'\n';
        message.Advance(1);
#endif
    }

#if FUSION
    private static void WriteBytesToChunks(List<byte[]> chunks, ref int currentPosition, ReadOnlySpan<byte> data)
    {
        var dataOffset = 0;

        while (dataOffset < data.Length)
        {
            if (chunks.Count == 0 || currentPosition >= JsonMemory.BufferSize)
            {
                currentPosition = 0;
                chunks.Add(JsonMemory.Rent());
            }

            var currentChunk = chunks[^1];
            var spaceInChunk = JsonMemory.BufferSize - currentPosition;
            var bytesToWrite = Math.Min(spaceInChunk, data.Length - dataOffset);

            var chunkSlice = currentChunk.AsSpan(currentPosition);
            data.Slice(dataOffset, bytesToWrite).CopyTo(chunkSlice);

            dataOffset += bytesToWrite;
            currentPosition += bytesToWrite;
        }
    }
#endif

#if FUSION
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMessageComplete(List<byte[]> chunks, int currentPosition)
    {
        if (chunks.Count == 0 || (chunks.Count == 1 && currentPosition < 2))
        {
            return false;
        }

        // If we have written more than two bytes into the current chunk then we can take the fast path
        // and skip the multi chunk handling.
        if (currentPosition >= 2)
        {
            var currentChunk = chunks[^1].AsSpan(0, currentPosition);
            return currentChunk[^1] == (byte)'\n' && currentChunk[^2] == (byte)'\n';
        }

        // If bytes are possible distributed across chunks we will need to inspect bytes in different chunks.
        return chunks[^1][0] == (byte)'\n' && chunks[^2][^1] == (byte)'\n';
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMessageComplete(ReadOnlySpan<byte> message)
        => message.Length >= 2 && message[^1] == (byte)'\n' && message[^2] == (byte)'\n';
#endif

#if FUSION
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsKeepAlive(List<byte[]> chunks, int currentPosition)
    {
        // The minimal keep alive message is 3 characters `:\n\n`.
        // Any message not starting with `:` or shorter than 3 lines is invalid/
        if (chunks.Count > 1 || currentPosition < 3 || chunks[0][0] != (byte)':')
        {
            return false;
        }

        var chunk = chunks[0].AsSpan(0, currentPosition);

        // Each message must end with to new lines, if we find none it's an invalid message.
        var firstNewline = chunk.IndexOf((byte)'\n');
        if (firstNewline == -1)
        {
            return false;
        }

        // After the ':', it should either be:
        // 1. End of line (just ":\n")
        // 2. A space followed by arbitrary text (": some text\n")
        // 3. Arbitrary text without space (":keep-alive\n")

        // But it should NOT contain any SSE field syntax like "event:" or "data:"
        // Check if the rest of the message (after this comment line) contains valid SSE fields
        var remaining = chunk[(firstNewline + 1)..];

        // If there's more content after the comment, it should be another \n (for message termination)
        // or it should be empty. Keep-alive messages shouldn't have event/data fields.
        return remaining.Length == 1 && remaining[0] == (byte)'\n';
    }
#else
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsKeepAlive(ReadOnlySpan<byte> message)
    {
        // The minimal keep alive message is 3 characters `:\n\n`.
        // Any message not starting with `:` or shorter than 3 lines is invalid/
        if (message.Length < 3 || message[0] != (byte)':')
        {
            return false;
        }

        // Each message must end with to new lines, if we find none it's an invalid message.
        var firstNewline = message.IndexOf((byte)'\n');
        if (firstNewline == -1)
        {
            return false;
        }

        // After the ':', it should either be:
        // 1. End of line (just ":\n")
        // 2. A space followed by arbitrary text (": some text\n")
        // 3. Arbitrary text without space (":keep-alive\n")

        // But it should NOT contain any SSE field syntax like "event:" or "data:"
        // Check if the rest of the message (after this comment line) contains valid SSE fields
        var remaining = message[(firstNewline + 1)..];

        // If there's more content after the comment, it should be another \n (for message termination)
        // or it should be empty. Keep-alive messages shouldn't have event/data fields.
        return remaining.Length == 1 && remaining[0] == (byte)'\n';
    }
#endif
}
