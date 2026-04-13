using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    private static readonly byte[][] s_emptyObject = ["{}"u8.ToArray()];

    internal static SourceResultDocument CreateEmptyObject()
        => Parse(s_emptyObject, 2, 1, pooledMemory: false);

    internal static SourceResultDocument Parse(
        byte[] data,
        int size)
        => Parse([data], size, usedChunks: 1, pooledMemory: true);

    internal static SourceResultDocument Parse(
        byte[][] dataChunks,
        int lastLength,
        int usedChunks,
        bool pooledMemory)
    {
        Debug.Assert(dataChunks is not null, "dataChunks cannot be null.");
        Debug.Assert(dataChunks.Length >= usedChunks, "dataChunks length must be >= usedChunks.");
        Debug.Assert(usedChunks > 0, "usedChunks must be > 0.");

        if (usedChunks == 1)
        {
            return ParseSingleSegment(dataChunks, lastLength, pooledMemory);
        }

        return ParseMultipleSegments(dataChunks, lastLength, usedChunks, pooledMemory);
    }

    internal static SourceResultDocument Parse(
        ref Utf8JsonReader reader,
        byte[][] dataChunks,
        int usedChunks,
        bool skipInitialRead,
        bool pooledMemory)
    {
        var metaDb = MetaDb.CreateForEstimatedRows(1);

        try
        {
            ParseJson(ref reader, ref metaDb, skipInitialRead);
        }
        catch
        {
            metaDb.Dispose();

            throw;
        }

        return new SourceResultDocument(metaDb, dataChunks, usedChunks, pooledMemory);
    }

    internal static SourceResultDocument ParseSingleSegment(
        byte[][] dataChunks,
        int lastLength,
        bool pooledMemory)
    {
        var dataChunksSpan = dataChunks.AsSpan(0, 1);
        var reader = new Utf8JsonReader(dataChunksSpan[0].AsSpan(0, lastLength));

        var totalBytes = CalculateTotalBytes(dataChunksSpan, lastLength);
        var estimatedTokens = Math.Max(totalBytes / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(estimatedTokens);

        try
        {
            ParseJson(ref reader, ref metaDb);
        }
        catch
        {
            metaDb.Dispose();

            if (pooledMemory && dataChunks.Length > 1)
            {
                foreach (var chunk in dataChunksSpan)
                {
                    JsonMemory.Return(JsonMemoryKind.Json, chunk);
                }

                dataChunksSpan.Clear();
                ArrayPool<byte[]>.Shared.Return(dataChunks);
            }

            throw;
        }

        return new SourceResultDocument(metaDb, dataChunks, 1, pooledMemory);
    }

    internal static SourceResultDocument ParseMultipleSegments(
        byte[][] dataChunks,
        int lastLength,
        int usedChunks,
        bool pooledMemory)
    {
        SequenceSegment? first = null;
        SequenceSegment? previous = null;
        var dataChunksSpan = dataChunks.AsSpan(0, usedChunks);

        for (var i = 0; i < dataChunksSpan.Length; i++)
        {
            var chunk = dataChunksSpan[i];
            var chunkDataLength = (i == dataChunksSpan.Length - 1) ? lastLength : JsonMemory.BufferSize;
            var current = new SequenceSegment(chunk, chunkDataLength);

            first ??= current;
            previous?.SetNext(current);
            previous = current;
        }

        if (first is null || previous is null)
        {
            throw new InvalidOperationException("Sequence segments cannot be empty.");
        }

        var sequence = new ReadOnlySequence<byte>(first, 0, previous, lastLength);
        var reader = new Utf8JsonReader(sequence);

        var totalBytes = CalculateTotalBytes(dataChunksSpan, lastLength);
        var estimatedTokens = Math.Max(totalBytes / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(estimatedTokens);

        try
        {
            ParseJson(ref reader, ref metaDb);
        }
        catch
        {
            metaDb.Dispose();

            if (pooledMemory && dataChunks.Length > 1)
            {
                foreach (var chunk in dataChunksSpan)
                {
                    JsonMemory.Return(JsonMemoryKind.Json, chunk);
                }

                dataChunksSpan.Clear();
                ArrayPool<byte[]>.Shared.Return(dataChunks);
            }

            throw;
        }

        return new SourceResultDocument(metaDb, dataChunks, usedChunks, pooledMemory);
    }

    private static void ParseJson(ref Utf8JsonReader reader, ref MetaDb metaDb, bool skipInitialRead = false)
    {
        Span<Cursor> containerStart = stackalloc Cursor[64];
        Span<int> containerStartLocation = stackalloc int[64];
        Span<int> containerChildCount = stackalloc int[64];
        var containerIsArrayBits = 0UL;
        var containerHasComplexBits = 0UL;
        var stackIndex = 0;

        while (skipInitialRead || reader.Read())
        {
            skipInitialRead = false;
            var tokenType = reader.TokenType;
            var location = (int)reader.TokenStartIndex;
            var tokenLength = (int)(reader.BytesConsumed - location);

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
        => ((end.Chunk - start.Chunk) * Cursor.RowsPerChunk) + end.Row - start.Row + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendStringToken(
        ref MetaDb metaDb,
        JsonTokenType tokenType,
        int startLocation,
        int tokenLength,
        Utf8JsonReader reader)
    {
        // For strings, skip the opening quote and reduce length to exclude both quotes
        var adjustedLocation = startLocation + 1;
        var adjustedLength = tokenLength - 2;

        metaDb.Append(
            tokenType,
            adjustedLocation,
            adjustedLength,
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateTotalBytes(Span<byte[]> dataChunks, int lastLength)
    {
        switch (dataChunks.Length)
        {
            case 0:
                return 0;

            case 1:
                return lastLength;

            default:
                // calculate the size of all full chunks
                var total = (dataChunks.Length - 1) * JsonMemory.BufferSize;

                // plus the partial last chunk
                total += lastLength;
                return total;
        }
    }
}
