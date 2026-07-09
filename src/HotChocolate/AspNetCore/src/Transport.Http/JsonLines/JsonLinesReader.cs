#if FUSION
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
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
internal class JsonLinesReader(HttpResponseMessage message, IMemoryArenaSource arenaSource) : IAsyncEnumerable<SourceResultDocument>
#else
internal class JsonLinesReader(HttpResponseMessage message) : IAsyncEnumerable<OperationResult>
#endif
{
#if FUSION
    private const int MaxSingleSpanRecordLength = 16 * 1024;

#endif
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
        await using var stream = await message.Content.ReadAsStreamAsync(cancellationToken);
        var reader = PipeReader.Create(stream, s_options);

        try
        {
            ReadResult result;
            do
            {
                result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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
                        yield return ParseDocument(arenaSource.GetNextArena(), line);
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
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

#if FUSION
    private static SourceResultDocument ParseDocument(IMemoryArena arena, ReadOnlySequence<byte> lineBuffer)
    {
        var lineLength = (int)lineBuffer.Length;

        // A record whose length fits within MaxSingleSpanRecordLength is filled once into a single
        // exact-length arena chunk and parsed in place as one span. This skips the geometric ramp fill
        // and the multi-segment reader it produces. The record length is known from newline framing, so
        // this does not depend on a Content-Length header.
        //
        // The threshold is capped at 16 KB because the request-scoped arena is shared by all concurrent
        // subgraph fetches and lives until the response is written. A large exact-length rent that does
        // not fit the current page strands the whole remaining page tail for the rest of the request,
        // while the geometric ramp's small leading chunks can fill those tails. Small records, which
        // cover all realistic traffic, get the single-span win. Large records keep the tail-filling ramp.
        if (lineLength > 0 && lineLength <= MaxSingleSpanRecordLength)
        {
            var buffer = arena.Rent(lineLength);
            lineBuffer.CopyTo(buffer.Span);

            var segments = arena.RentSegmentTable(1);
            segments[0] = buffer;

            return SourceResultDocument.ParseFilled(
                arena,
                segments,
                usedChunks: 1,
                lastLength: lineLength);
        }

        // Fallback: larger records (and the degenerate empty line) are filled across the geometric data
        // chunk schedule and parsed as a multi-segment sequence, using the same per-record mechanic as
        // the SSE reader.
        var chunks = arena.RentSegmentTable(64);
        var chunkIndex = 0;
        var chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
        var current = chunks[chunkIndex] = arena.Rent(chunkSize);
        var currentChunkPosition = 0;

        foreach (var segment in lineBuffer)
        {
            var source = segment.Span;
            var segmentOffset = 0;

            while (segmentOffset < source.Length)
            {
                var spaceInCurrentChunk = chunkSize - currentChunkPosition;
                var bytesToCopy = Math.Min(spaceInCurrentChunk, source.Length - segmentOffset);

                source.Slice(segmentOffset, bytesToCopy).CopyTo(current.Span.Slice(currentChunkPosition));
                currentChunkPosition += bytesToCopy;
                segmentOffset += bytesToCopy;

                if (currentChunkPosition == chunkSize)
                {
                    if (chunkIndex + 1 >= SourceResultDocument.DataMaxChunks)
                    {
                        throw new InvalidOperationException(
                            "The source result document has exceeded its maximum data capacity.");
                    }

                    if (chunkIndex + 1 >= chunks.Length)
                    {
                        arena.GrowSegmentTable(ref chunks);
                    }

                    chunkIndex++;
                    chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
                    current = chunks[chunkIndex] = arena.Rent(chunkSize);
                    currentChunkPosition = 0;
                }
            }
        }

        return SourceResultDocument.ParseFilled(
            arena,
            chunks,
            usedChunks: chunkIndex + 1,
            lastLength: currentChunkPosition);
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
