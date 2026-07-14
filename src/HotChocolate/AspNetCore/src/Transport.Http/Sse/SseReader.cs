using System.Net.ServerSentEvents;
#if FUSION
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

#if FUSION
internal sealed class SseReader(HttpResponseMessage message, IMemoryArenaSource arenaSource)
    : IAsyncEnumerable<SourceResultDocument>
#else
internal sealed class SseReader(HttpResponseMessage message)
    : IAsyncEnumerable<OperationResult>
#endif
{
    // The GraphQL over SSE protocol uses the "next" event to carry a result and the "complete" event
    // to terminate the subscription. Any other event type (and keep-alive comments) is ignored.
    private const string NextEvent = "next";
    private const string CompleteEvent = "complete";

#if FUSION
    private const int MaxSingleSpanRecordLength = 16 * 1024;

#endif
#if !FUSION
    private static readonly SseItemParser<OperationResult?> s_itemParser =
        static (eventType, data) =>
            eventType == NextEvent && data.Length > 0
                ? OperationResult.Parse(data)
                : null;
#endif

#if FUSION
    public async IAsyncEnumerator<SourceResultDocument> GetAsyncEnumerator(
#else
    public async IAsyncEnumerator<OperationResult> GetAsyncEnumerator(
#endif
        CancellationToken cancellationToken = default)
    {
        await using var stream = await message.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

#if FUSION
        // Only a "next" event produces a document: its payload bytes are filled exactly once into a
        // freshly acquired arena and parsed in place. The parser is pull-based and never invokes this
        // delegate for the following event before the current one has been consumed, so the arena
        // acquired here pairs with the document the consumer is about to read.
        var parser = SseParser.Create(
            stream,
            (eventType, data) =>
            {
                if (eventType != NextEvent || data.Length == 0)
                {
                    return (SourceResultDocument?)null;
                }

                return FillAndParse(arenaSource.GetNextArena(), data);
            });
#else
        var parser = SseParser.Create(stream, s_itemParser);
#endif

        await foreach (var item in parser.EnumerateAsync(cancellationToken).ConfigureAwait(false))
        {
            // A "complete" event terminates the stream; any accompanying data is ignored. A bare
            // "complete" event carries no data, so it produces no item and the stream ends at the
            // end of the response instead.
            if (item.EventType == CompleteEvent)
            {
                yield break;
            }

            if (item.Data is { } result)
            {
                yield return result;
            }
        }
    }

#if FUSION
    private static SourceResultDocument FillAndParse(IMemoryArena arena, ReadOnlySpan<byte> data)
    {
        if (data.Length > 0 && data.Length <= MaxSingleSpanRecordLength)
        {
            var buffer = arena.Rent(data.Length);
            data.CopyTo(buffer.Span);

            var segments = arena.RentSegmentTable(1);
            segments[0] = buffer;

            return SourceResultDocument.ParseFilled(
                arena,
                segments,
                usedChunks: 1,
                lastLength: data.Length);
        }

        // The payload is filled once, directly into the arena's gap-free geometric segments using the
        // same per-event mechanic as the other streaming readers, then parsed in place via ParseFilled
        // so no bytes are copied a second time.
        var chunks = arena.RentSegmentTable(64);
        var chunkIndex = 0;
        var chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
        var current = chunks[chunkIndex] = arena.Rent(chunkSize);
        var currentChunkPosition = 0;
        var dataOffset = 0;

        while (dataOffset < data.Length)
        {
            var spaceInCurrentChunk = chunkSize - currentChunkPosition;
            var bytesToCopy = Math.Min(spaceInCurrentChunk, data.Length - dataOffset);

            data.Slice(dataOffset, bytesToCopy).CopyTo(current.Span.Slice(currentChunkPosition));
            currentChunkPosition += bytesToCopy;
            dataOffset += bytesToCopy;

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

        return SourceResultDocument.ParseFilled(
            arena,
            chunks,
            usedChunks: chunkIndex + 1,
            lastLength: currentChunkPosition);
    }
#endif
}
