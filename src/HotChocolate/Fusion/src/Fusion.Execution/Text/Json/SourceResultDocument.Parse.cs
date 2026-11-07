using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    private static readonly byte[] s_emptyObject = "{}"u8.ToArray();

    internal static SourceResultDocument CreateEmptyObject(IMemoryArena arena)
        => Parse(arena, s_emptyObject, 2);

    internal static SourceResultDocument Parse(
        IMemoryArena arena,
        byte[] data,
        int size)
    {
        ArgumentNullException.ThrowIfNull(arena);

        // A single foreign buffer that fits the 17-bit offset field is adopted as chunk 0 and
        // parsed in place, so no bytes are copied. Token start positions are gap-free linear offsets
        // that map directly to packed (0, offset) data locations.
        if (size <= DataOffsetMask + 1)
        {
            var segments = arena.RentSegmentTable(1);
            segments[0] = new MemorySegment(data, 0, size);

            var estimatedTokens = Math.Max(size / 12, 100);
            var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);
            var document = default(SingleDocument);

            try
            {
                ParseJson<SingleWindow, SingleDocument>(
                    data.AsSpan(0, size),
                    segments: null,
                    size,
                    singleChunk: true,
                    ref metaDb,
                    ref document,
                    ArrayPool<byte>.Shared);
            }
            catch
            {
                metaDb.Dispose();
                throw;
            }

            return new SourceResultDocument(metaDb, segments, usedChunks: 1);
        }

        return Parse(arena, new ReadOnlySequence<byte>(data, 0, size));
    }

    internal static SourceResultDocument Parse(
        IMemoryArena arena,
        byte[][] dataChunks,
        int lastLength,
        int usedChunks)
    {
        Debug.Assert(dataChunks is not null, "dataChunks cannot be null.");
        Debug.Assert(dataChunks.Length >= usedChunks, "dataChunks length must be >= usedChunks.");
        Debug.Assert(usedChunks > 0, "usedChunks must be > 0.");

        if (usedChunks == 1)
        {
            return Parse(arena, new ReadOnlySequence<byte>(dataChunks[0], 0, lastLength));
        }

        return Parse(arena, BuildSequence(dataChunks, lastLength, usedChunks));
    }

    internal static SourceResultDocument Parse(
        IMemoryArena arena,
        ReadOnlySequence<byte> input)
    {
        ArgumentNullException.ThrowIfNull(arena);

        // Copy the raw payload into the document's own geometric arena chunks without gaps. The
        // token locations are the same in the copy, so a contiguous input is parsed in place.
        var segments = IngestToArena(arena, input, out var usedChunks, out var lastLength);

        if (!input.IsSingleSegment)
        {
            return ParseFilled(arena, segments, usedChunks, lastLength);
        }

        var length = (int)input.Length;
        var estimatedTokens = Math.Max(length / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);
        var document = default(SingleDocument);

        try
        {
            ParseJson<SingleWindow, SingleDocument>(
                input.FirstSpan,
                segments: null,
                length,
                singleChunk: usedChunks == 1,
                ref metaDb,
                ref document,
                ArrayPool<byte>.Shared);
        }
        catch
        {
            metaDb.Dispose();
            throw;
        }

        return new SourceResultDocument(metaDb, segments, usedChunks);
    }

    /// <summary>
    /// Parses a document whose payload has already been filled into the document's own gap-free
    /// geometric arena chunks, recorded as <paramref name="segments"/>. The chunks must follow the
    /// data chunk schedule (chunk i holds <see cref="GetDataChunkSize"/> bytes) so token positions map
    /// to packed data locations, <paramref name="usedChunks"/> is the number of filled chunks, and
    /// <paramref name="lastLength"/> is the number of bytes used in the final chunk.
    /// </summary>
    internal static SourceResultDocument ParseFilled(
        IMemoryArena arena,
        MemorySegment[] segments,
        int usedChunks,
        int lastLength)
        => ParseFilled(arena, segments, usedChunks, lastLength, ArrayPool<byte>.Shared);

    internal static SourceResultDocument ParseFilled(
        IMemoryArena arena,
        MemorySegment[] segments,
        int usedChunks,
        int lastLength,
        ArrayPool<byte> bufferPool)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(bufferPool);
        Debug.Assert(usedChunks > 0, "data must contain at least one chunk.");

        var length = GetFilledLength(segments, usedChunks, lastLength);
        var estimatedTokens = Math.Max(length / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);
        var document = default(SingleDocument);

        try
        {
            ParseJson(segments, usedChunks, length, ref metaDb, ref document, bufferPool);
        }
        catch
        {
            metaDb.Dispose();
            throw;
        }

        return new SourceResultDocument(metaDb, segments, usedChunks);
    }

    /// <summary>
    /// Parses each element of the JSON array in the filled data chunks <paramref name="segments"/> into
    /// its own document over those chunks, with the chunk requirements of
    /// <see cref="ParseFilled(IMemoryArena, MemorySegment[], int, int)"/>.
    /// The input after the closing bracket of the array is not read.
    /// </summary>
    internal static List<SourceResultDocument> ParseFilledArrayElements(
        IMemoryArena arena,
        MemorySegment[] segments,
        int usedChunks,
        int lastLength)
        => ParseFilledArrayElements(arena, segments, usedChunks, lastLength, ArrayPool<byte>.Shared);

    internal static List<SourceResultDocument> ParseFilledArrayElements(
        IMemoryArena arena,
        MemorySegment[] segments,
        int usedChunks,
        int lastLength,
        ArrayPool<byte> bufferPool)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(bufferPool);
        Debug.Assert(usedChunks > 0, "data must contain at least one chunk.");

        var length = GetFilledLength(segments, usedChunks, lastLength);
        var metaDb = default(MetaDb);
        var documents = new ElementDocuments(arena, segments, usedChunks);

        try
        {
            ParseJson(segments, usedChunks, length, ref metaDb, ref documents, bufferPool);
        }
        catch
        {
            documents.Dispose(ref metaDb);
            throw;
        }

        return documents.Documents;
    }

    /// <summary>
    /// Gets the filled span of the first segment, used to peek at the leading JSON token. Only the
    /// last segment is partially filled, so the first segment is bounded by
    /// <paramref name="lastLength"/> when it is also the last one.
    /// </summary>
    internal static ReadOnlySpan<byte> FirstSpan(MemorySegment[] segments, int usedChunks, int lastLength)
    {
        if (usedChunks == 0)
        {
            return default;
        }

        var seg = segments[0];
        var length = usedChunks == 1 ? lastLength : seg.Length;
        return seg.Buffer.AsSpan(seg.Offset, length);
    }

    /// <summary>
    /// Copies the input payload into geometric arena chunks without gaps so the chunk schedule
    /// matches the packed data-location encoding, and returns the rented segment table.
    /// <paramref name="lastLength"/> is the number of bytes filled in the last used chunk.
    /// </summary>
    private static MemorySegment[] IngestToArena(
        IMemoryArena arena,
        ReadOnlySequence<byte> input,
        out int usedChunks,
        out int lastLength)
    {
        var segments = arena.RentSegmentTable(16);
        var totalLength = input.Length;

        if (totalLength == 0)
        {
            // An empty payload still needs a backing chunk so location 0 is addressable.
            segments[0] = arena.Rent(GetDataChunkSize(0));
            usedChunks = 1;
            lastLength = 0;
            return segments;
        }

        // The next chunk to allocate; the current fill chunk is segments[chunkIndex].
        var nextChunkIndex = 0;
        var chunkIndex = -1;
        var current = default(MemorySegment);
        var chunkSize = 0;
        var offset = 0;

        foreach (var inputSegment in input)
        {
            var source = inputSegment.Span;

            while (source.Length > 0)
            {
                if (offset == chunkSize)
                {
                    if (nextChunkIndex >= DataMaxChunks)
                    {
                        throw ThrowHelper.SourceResultDocumentTooLarge();
                    }

                    chunkSize = GetDataChunkSize(nextChunkIndex);
                    chunkIndex++;

                    if (chunkIndex >= segments.Length)
                    {
                        arena.GrowSegmentTable(ref segments);
                    }

                    current = segments[chunkIndex] = arena.Rent(chunkSize);
                    nextChunkIndex++;
                    offset = 0;
                }

                var toCopy = Math.Min(source.Length, chunkSize - offset);
                source[..toCopy].CopyTo(current.Span[offset..]);
                source = source[toCopy..];
                offset += toCopy;
            }
        }

        usedChunks = chunkIndex + 1;
        lastLength = offset;
        return segments;
    }

    private static int GetFilledLength(MemorySegment[] segments, int usedChunks, int lastLength)
    {
        var length = lastLength;

        for (var i = 0; i < usedChunks - 1; i++)
        {
            length += segments[i].Length;
        }

        return length;
    }

    private static ReadOnlySequence<byte> BuildSequence(byte[][] dataChunks, int lastLength, int usedChunks)
    {
        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < usedChunks; i++)
        {
            var chunkDataLength = i == usedChunks - 1 ? lastLength : dataChunks[i].Length;
            var current = new SequenceSegment(dataChunks[i], chunkDataLength);

            first ??= current;
            previous?.SetNext(current);
            previous = current;
        }

        if (first is null || previous is null)
        {
            throw new InvalidOperationException("Sequence segments cannot be empty.");
        }

        return new ReadOnlySequence<byte>(first, 0, previous, lastLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CloseContainer(
        ref MetaDb metaDb,
        JsonTokenType startTokenType,
        JsonTokenType endTokenType,
        Cursor startCursor,
        int startLocation,
        int endLocation,
        int sizeOrLength,
        bool hasComplexChildren)
    {
        // NextCursor is where the end row will land, so the inclusive
        // distance from the start cursor is the total row count.
        var endCursor = metaDb.NextCursor;
        var rows = CursorDistance(startCursor, endCursor);

        metaDb.Replace(
            startCursor,
            startTokenType,
            startLocation,
            sizeOrLength,
            rows,
            hasComplexChildren);

        metaDb.Append(endTokenType, endLocation, sizeOrLength, rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CursorDistance(Cursor start, Cursor end)
    {
        if (start.Chunk == end.Chunk)
        {
            return end.Row - start.Row + 1;
        }

        return end.Index - start.Index + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendNumberToken(
        ref MetaDb metaDb,
        int startLocation,
        int tokenLength)
        => metaDb.Append(
            JsonTokenType.Number,
            startLocation,
            tokenLength,
            hasComplexChildren: false);
}
