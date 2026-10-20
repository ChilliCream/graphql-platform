using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Transport.Http;

/// <summary>
/// Reads a JSON Lines response directly into one arena and parses each record in place as soon as
/// its terminating newline has arrived. The records share the arena chunks of the body.
/// </summary>
internal sealed class ArenaJsonLinesReader(
    HttpResponseMessage message,
    IMemoryArenaSource arenaSource,
    TimeSpan readTimeout)
    : IAsyncEnumerable<SourceResultDocument>
{
    public async IAsyncEnumerator<SourceResultDocument> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        var responseStream = await message.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var stream = readTimeout == Timeout.InfiniteTimeSpan
            ? responseStream
            : new ReadTimeoutStream(responseStream, readTimeout);

        var arena = arenaSource.GetNextArena();
        var chunks = arena.RentSegmentTable(64);
        var chunkIndex = 0;
        var chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
        var chunkStart = 0L;
        var current = chunks[chunkIndex] = arena.Rent(chunkSize);
        var position = 0;
        var recordStart = 0L;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = await stream
                .ReadAsync(current.Memory[position..], cancellationToken)
                .ConfigureAwait(false);

            if (bytesRead == 0)
            {
                break;
            }

            var scanStart = position;
            position += bytesRead;

            while (TryFindNewLine(current, scanStart, position, out var newLine))
            {
                var recordEnd = chunkStart + newLine;

                if (TryParseRecord(arena, chunks, chunkIndex + 1, recordStart, recordEnd, out var document))
                {
                    yield return document;
                }

                recordStart = recordEnd + 1;
                scanStart = newLine + 1;
            }

            if (position == chunkSize)
            {
                if (chunkIndex + 1 >= SourceResultDocument.DataMaxChunks)
                {
                    throw Text.Json.ThrowHelper.SourceResultDocumentTooLarge();
                }

                if (chunkIndex + 1 >= chunks.Length)
                {
                    // Documents already handed out keep referencing the current table,
                    // so it is not returned to the pool before the arena is disposed.
                    var grown = arena.RentSegmentTable(chunks.Length * 2);
                    Array.Copy(chunks, grown, chunkIndex + 1);
                    chunks = grown;
                }

                chunkStart += chunkSize;
                chunkIndex++;
                chunkSize = SourceResultDocument.GetDataChunkSize(chunkIndex);
                current = chunks[chunkIndex] = arena.Rent(chunkSize);
                position = 0;
            }
        }

        // A last record without a terminating newline.
        if (TryParseRecord(arena, chunks, chunkIndex + 1, recordStart, chunkStart + position, out var last))
        {
            yield return last;
        }
    }

    private static bool TryFindNewLine(MemorySegment chunk, int start, int end, out int newLine)
    {
        var index = chunk.Span[start..end].IndexOf((byte)'\n');

        if (index == -1)
        {
            newLine = -1;
            return false;
        }

        newLine = start + index;
        return true;
    }

    private static bool TryParseRecord(
        IMemoryArena arena,
        MemorySegment[] chunks,
        int usedChunks,
        long start,
        long end,
        [NotNullWhen(true)] out SourceResultDocument? document)
    {
        if (IsWhiteSpace(chunks, start, end))
        {
            document = null;
            return false;
        }

        document = SourceResultDocument.ParseFilledRange(arena, chunks, usedChunks, start, (int)(end - start));
        return true;
    }

    private static bool IsWhiteSpace(MemorySegment[] chunks, long start, long end)
    {
        var chunkIndex = 0;
        var chunkStart = 0L;
        var chunkSize = SourceResultDocument.GetDataChunkSize(0);

        while (start < end)
        {
            while (start >= chunkStart + chunkSize)
            {
                chunkStart += chunkSize;
                chunkSize = SourceResultDocument.GetDataChunkSize(++chunkIndex);
            }

            var offset = (int)(start - chunkStart);
            var length = (int)Math.Min(end - start, chunkSize - offset);

            if (!IsWhiteSpace(chunks[chunkIndex].Span.Slice(offset, length)))
            {
                return false;
            }

            start += length;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsWhiteSpace(ReadOnlySpan<byte> span)
        => span.IndexOfAnyExcept((byte)' ', (byte)'\t', (byte)'\r') == -1;
}
