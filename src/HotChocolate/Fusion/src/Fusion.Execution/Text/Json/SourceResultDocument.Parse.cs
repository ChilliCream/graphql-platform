using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

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
        // parsed in place, so no bytes are copied. Token start positions reported by the reader are
        // gap-free linear offsets that map directly to packed (0, offset) data locations.
        if (size <= DataOffsetMask + 1)
        {
            var segments = arena.RentSegmentTable(1);
            segments[0] = new MemorySegment(data, 0, size);

            var reader = new Utf8JsonReader(data.AsSpan(0, size));
            var estimatedTokens = Math.Max(size / 12, 100);
            var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);

            try
            {
                ParseJson(ref reader, ref metaDb, singleChunk: true);
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

        // Copy the raw payload into the document's own geometric arena chunks without gaps and
        // build a reader over them. Token start positions reported by the reader are gap-free
        // linear offsets, which are mapped to packed (chunk, offset) data locations.
        var segments = IngestToArena(arena, input, out var usedChunks);

        var reader = new Utf8JsonReader(BuildSequence(segments, usedChunks));
        var totalBytes = input.Length;
        var estimatedTokens = (int)Math.Max(totalBytes / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);

        try
        {
            ParseJson(ref reader, ref metaDb, singleChunk: usedChunks == 1);
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
    {
        ArgumentNullException.ThrowIfNull(arena);
        Debug.Assert(usedChunks > 0, "data must contain at least one chunk.");

        var sequence = BuildFilledSequence(segments, usedChunks, lastLength);
        var reader = new Utf8JsonReader(sequence);
        var estimatedTokens = (int)Math.Max(sequence.Length / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(arena, estimatedTokens);

        try
        {
            ParseJson(ref reader, ref metaDb, singleChunk: usedChunks == 1);
        }
        catch
        {
            metaDb.Dispose();
            throw;
        }

        return new SourceResultDocument(metaDb, segments, usedChunks);
    }

    /// <summary>
    /// Builds a reader over already-filled gap-free arena segments so several documents can be
    /// parsed in place over the same shared storage (for example an array of batched results).
    /// </summary>
    internal static Utf8JsonReader CreateFilledReader(MemorySegment[] segments, int usedChunks, int lastLength)
        => new(BuildFilledSequence(segments, usedChunks, lastLength));

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
    /// Parses a single JSON value that the reader is currently positioned at over the shared
    /// gap-free arena segments <paramref name="segments"/>. The produced document's packed data
    /// locations point into those shared segments, so no bytes are copied.
    /// </summary>
    internal static SourceResultDocument ParseFilledElement(
        IMemoryArena arena,
        ref Utf8JsonReader reader,
        MemorySegment[] segments,
        int usedChunks)
    {
        ArgumentNullException.ThrowIfNull(arena);

        var metaDb = MetaDb.CreateForEstimatedRows(arena, 1);

        try
        {
            ParseJson(ref reader, ref metaDb, skipInitialRead: true, singleChunk: usedChunks == 1);
        }
        catch
        {
            metaDb.Dispose();
            throw;
        }

        return new SourceResultDocument(metaDb, segments, usedChunks);
    }

    /// <summary>
    /// Copies the input payload into geometric arena chunks without gaps so the chunk schedule
    /// matches the packed data-location encoding, and returns the rented segment table.
    /// </summary>
    private static MemorySegment[] IngestToArena(
        IMemoryArena arena,
        ReadOnlySequence<byte> input,
        out int usedChunks)
    {
        var segments = arena.RentSegmentTable(64);
        var totalLength = input.Length;

        if (totalLength == 0)
        {
            // An empty payload still needs a backing chunk so location 0 is addressable.
            segments[0] = arena.Rent(GetDataChunkSize(0));
            usedChunks = 1;
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
                        throw new InvalidOperationException(
                            "The source result document has exceeded its maximum data capacity.");
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
                source[..toCopy].CopyTo(current.Span.Slice(offset));
                source = source[toCopy..];
                offset += toCopy;
            }
        }

        usedChunks = chunkIndex + 1;
        return segments;
    }

    private static ReadOnlySequence<byte> BuildFilledSequence(
        MemorySegment[] segments,
        int usedChunks,
        int lastLength)
    {
        if (usedChunks == 1)
        {
            return new ReadOnlySequence<byte>(segments[0].Memory[..lastLength]);
        }

        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < usedChunks; i++)
        {
            var length = i == usedChunks - 1 ? lastLength : segments[i].Length;
            var current = new SequenceSegment(segments[i].Memory[..length]);
            first ??= current;
            previous?.SetNext(current);
            previous = current;
        }

        return new ReadOnlySequence<byte>(first!, 0, previous!, lastLength);
    }

    private static ReadOnlySequence<byte> BuildSequence(MemorySegment[] segments, int usedChunks)
    {
        if (usedChunks == 1)
        {
            return new ReadOnlySequence<byte>(segments[0].Memory);
        }

        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < usedChunks; i++)
        {
            var current = new SequenceSegment(segments[i].Memory);
            first ??= current;
            previous?.SetNext(current);
            previous = current;
        }

        return new ReadOnlySequence<byte>(first!, 0, previous!, previous!.Memory.Length);
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

    private static void ParseJson(
        ref Utf8JsonReader reader,
        ref MetaDb metaDb,
        bool skipInitialRead = false,
        bool singleChunk = false)
    {
        Span<Cursor> containerStart = stackalloc Cursor[64];
        Span<int> containerStartLocation = stackalloc int[64];
        Span<int> containerChildCount = stackalloc int[64];
        var containerIsArrayBits = 0UL;
        var containerHasComplexBits = 0UL;
        var stackIndex = 0;

        // Token start positions are reported as monotonically increasing gap-free linear offsets, so
        // the boundary of the chunk a token falls into is tracked incrementally instead of walking the
        // geometric ramp per token. The walk advances on the first token (seeding from the reader's
        // starting position) and only rolls forward as later tokens cross a chunk boundary.
        var chunkIndex = 0;
        var chunkStartLinear = 0L;
        var chunkEndLinear = (long)GetDataChunkSize(0);

        while (skipInitialRead || reader.Read())
        {
            skipInitialRead = false;
            var tokenType = reader.TokenType;
            var linearStart = reader.TokenStartIndex;
            var tokenLength = (int)(reader.BytesConsumed - linearStart);

            // A single chunk holds the whole payload, so the gap-free linear offset is already the
            // packed (0, offset) location and the geometric ramp walk can be skipped.
            int location;

            if (singleChunk)
            {
                location = EncodeDataLocation(0, (int)linearStart);
            }
            else
            {
                while (linearStart >= chunkEndLinear)
                {
                    chunkStartLinear = chunkEndLinear;
                    chunkEndLinear += GetDataChunkSize(++chunkIndex);
                }

                location = EncodeDataLocation(chunkIndex, (int)(linearStart - chunkStartLinear));
            }

            if (tokenType == JsonTokenType.PropertyName)
            {
                // Remove the colon
                tokenLength--;
            }

            // Count direct children incrementally.
            if (stackIndex > 0)
            {
                if (tokenType == JsonTokenType.PropertyName)
                {
                    containerChildCount[stackIndex - 1]++;
                }
                else if ((containerIsArrayBits & (1UL << (stackIndex - 1))) != 0
                    && tokenType is not (JsonTokenType.EndObject or JsonTokenType.EndArray))
                {
                    containerChildCount[stackIndex - 1]++;
                }
            }

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                    // The new container is a complex child of its parent.
                    if (stackIndex > 0)
                    {
                        containerHasComplexBits |= 1UL << (stackIndex - 1);
                    }

                    containerStart[stackIndex] = metaDb.Reserve();
                    containerStartLocation[stackIndex] = location;
                    containerChildCount[stackIndex] = 0;
                    containerIsArrayBits &= ~(1UL << stackIndex);
                    containerHasComplexBits &= ~(1UL << stackIndex);
                    stackIndex++;
                    break;

                case JsonTokenType.EndObject:
                    --stackIndex;
                    CloseContainer(
                        ref metaDb,
                        JsonTokenType.StartObject,
                        JsonTokenType.EndObject,
                        containerStart[stackIndex],
                        containerStartLocation[stackIndex],
                        location,
                        containerChildCount[stackIndex],
                        (containerHasComplexBits & (1UL << stackIndex)) != 0);

                    if (stackIndex == 0)
                    {
                        return;
                    }
                    break;

                case JsonTokenType.StartArray:
                    if (stackIndex > 0)
                    {
                        containerHasComplexBits |= 1UL << (stackIndex - 1);
                    }

                    containerStart[stackIndex] = metaDb.Reserve();
                    containerStartLocation[stackIndex] = location;
                    containerChildCount[stackIndex] = 0;
                    containerIsArrayBits |= 1UL << stackIndex;
                    containerHasComplexBits &= ~(1UL << stackIndex);
                    stackIndex++;
                    break;

                case JsonTokenType.EndArray:
                    --stackIndex;
                    CloseContainer(
                        ref metaDb,
                        JsonTokenType.StartArray,
                        JsonTokenType.EndArray,
                        containerStart[stackIndex],
                        containerStartLocation[stackIndex],
                        location,
                        containerChildCount[stackIndex],
                        (containerHasComplexBits & (1UL << stackIndex)) != 0);
                    break;

                case JsonTokenType.PropertyName:
                case JsonTokenType.String:
                    AppendStringToken(
                        ref metaDb,
                        tokenType,
                        location,
                        tokenLength,
                        reader);
                    break;

                case JsonTokenType.Number:
                    AppendNumberToken(
                        ref metaDb,
                        location,
                        tokenLength,
                        reader);
                    break;

                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    metaDb.Append(tokenType, location, tokenLength);
                    break;

                default:
                    throw new JsonException($"Unexpected token type: {tokenType}");
            }
        }

        Debug.Assert(stackIndex == 0, "Unclosed containers remain");
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
    private static void AppendStringToken(
        ref MetaDb metaDb,
        JsonTokenType tokenType,
        int startLocation,
        int tokenLength,
        Utf8JsonReader reader)
    {
        // Strings and property names are stored quote-inclusive: the location points at the opening
        // quote and the length covers both quotes. This keeps every stored location pointing at a
        // real byte, so the surrounding quotes are recovered by slicing the value rather than by
        // arithmetic on the packed location.
        metaDb.Append(
            tokenType,
            startLocation,
            tokenLength,
            hasComplexChildren: ContainsEscapeSequences(reader));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendNumberToken(
        ref MetaDb metaDb,
        int startLocation,
        int tokenLength,
        Utf8JsonReader reader)
        => metaDb.Append(
            JsonTokenType.Number,
            startLocation,
            tokenLength,
            hasComplexChildren: ContainsScientificNotation(reader.ValueSpan));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsEscapeSequences(Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            foreach (var segment in reader.ValueSequence)
            {
                if (segment.Span.IndexOf(JsonConstants.BackSlash) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        return reader.ValueSpan.IndexOf(JsonConstants.BackSlash) is not -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsScientificNotation(ReadOnlySpan<byte> value)
        => value.IndexOfAny((byte)'e', (byte)'E') >= 0;
}
