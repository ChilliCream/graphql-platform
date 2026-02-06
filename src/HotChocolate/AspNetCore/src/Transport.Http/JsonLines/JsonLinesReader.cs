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
internal class JsonLinesReader(HttpResponseMessage message) : IAsyncEnumerable<SourceResultDocument>
#else
internal class JsonLinesReader(HttpResponseMessage message) : IAsyncEnumerable<OperationResult>
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

                    var line = buffer.Slice(0, position.Value);

                    // Skip empty lines
                    if (!IsEmptyLine(line))
                    {
#if FUSION
                        yield return ParseDocument(line);
#else
                        var document = ParseDocument(line);
                        yield return OperationResult.Parse(document);
#endif
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
        }
    }

#if FUSION
    private static SourceResultDocument ParseDocument(ReadOnlySequence<byte> lineBuffer)
    {
        var requiredSize = (int)lineBuffer.Length;

        // Ceiling division to make sure we end up with the right amount of chunks.
        var chunksNeeded = (requiredSize + JsonMemory.BufferSize - 1) / JsonMemory.BufferSize;
        var chunks = JsonMemory.RentRange(chunksNeeded);
        var chunkIndex = 0;
        var chunkPosition = 0;

        // Copy lineBuffer data into pre-allocated chunks
        if (lineBuffer.IsSingleSegment)
        {
            var span = lineBuffer.First.Span;
            WriteBytesToChunks(chunks, ref chunkIndex, ref chunkPosition, span);
        }
        else
        {
            var position = lineBuffer.Start;
            while (lineBuffer.TryGet(ref position, out var memory))
            {
                WriteBytesToChunks(chunks, ref chunkIndex, ref chunkPosition, memory.Span);
            }
        }

        var lastBufferSize = requiredSize % JsonMemory.BufferSize;
        if (lastBufferSize == 0 && chunks.Length > 0)
        {
            lastBufferSize = JsonMemory.BufferSize;
        }

        return SourceResultDocument.Parse(
            chunks,
            lastBufferSize,
            chunksNeeded,
            pooledMemory: true);
    }

    private static void WriteBytesToChunks(
        byte[][] chunks,
        ref int chunkIndex,
        ref int chunkPosition,
        ReadOnlySpan<byte> data)
    {
        var dataOffset = 0;

        while (dataOffset < data.Length)
        {
            if (chunkPosition >= JsonMemory.BufferSize)
            {
                chunkPosition = 0;
                chunkIndex++;
            }

            var currentChunk = chunks[chunkIndex];
            var spaceInChunk = JsonMemory.BufferSize - chunkPosition;
            var bytesToWrite = Math.Min(spaceInChunk, data.Length - dataOffset);

            data.Slice(dataOffset, bytesToWrite).CopyTo(currentChunk.AsSpan(chunkPosition));

            dataOffset += bytesToWrite;
            chunkPosition += bytesToWrite;
        }
    }
#else
    private static JsonDocumentOwner ParseDocument(ReadOnlySequence<byte> lineBuffer)
    {
        var requiredSize = (int)lineBuffer.Length;
        var buffer = new PooledArrayWriter(requiredSize);
        lineBuffer.CopyTo(buffer.GetSpan(requiredSize));
        buffer.Advance(requiredSize);
        return new JsonDocumentOwner(JsonDocument.Parse(buffer.WrittenMemory), buffer);
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEmptyLine(ReadOnlySequence<byte> lineBuffer)
    {
        if (lineBuffer.IsEmpty)
        {
            return true;
        }

        if (lineBuffer.IsSingleSegment)
        {
            var span = lineBuffer.First.Span;
            return IsWhitespaceOnly(span);
        }

        var position = lineBuffer.Start;
        while (lineBuffer.TryGet(ref position, out var memory))
        {
            if (!IsWhitespaceOnly(memory.Span))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhitespaceOnly(ReadOnlySpan<byte> span)
    {
        foreach (var b in span)
        {
            if (b != (byte)' ' && b != (byte)'\t' && b != (byte)'\r')
            {
                return false;
            }
        }
        return true;
    }
}
