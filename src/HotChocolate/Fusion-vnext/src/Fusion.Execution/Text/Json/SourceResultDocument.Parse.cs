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

            switch (tokenType)
            {
                case JsonTokenType.StartObject:
                {
                    var startCursor = metaDb.Append(tokenType, location);
                    containerStart[stackIndex++] = startCursor;
                    break;
                }

                case JsonTokenType.EndObject:
                {
                    var startCursor = containerStart[--stackIndex];
                    CloseObject(ref metaDb, startCursor, location);

                    if (stackIndex == 0)
                    {
                        return;
                    }
                    break;
                }

                case JsonTokenType.StartArray:
                {
                    var startCursor = metaDb.Append(tokenType, location);
                    containerStart[stackIndex++] = startCursor;
                    break;
                }

                case JsonTokenType.EndArray:
                {
                    var startCursor = containerStart[--stackIndex];
                    CloseArray(ref metaDb, startCursor, location);
                    break;
                }

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
    private static void CloseObject(ref MetaDb metaDb, Cursor startId, int location)
    {
        var startRow = metaDb.Get(startId);

        if (startRow.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected {JsonTokenType.StartObject} but found {startRow.TokenType}.");
        }

        var endId = metaDb.Append(JsonTokenType.EndObject, location);
        var (properties, rows) = CalculateObjectPropertyCount(ref metaDb, startId, endId);

        metaDb.SetLength(startId, properties);
        metaDb.SetNumberOfRows(startId, rows);
        metaDb.SetLength(endId, properties);
        metaDb.SetNumberOfRows(endId, rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CloseArray(ref MetaDb metaDb, Cursor startCursor, int location)
    {
        var startRow = metaDb.Get(startCursor);

        if (startRow.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected {JsonTokenType.StartArray} but found {startRow.TokenType}.");
        }

        var endId = metaDb.Append(JsonTokenType.EndArray, location);
        var (elements, rows) = CalculateArrayCounts(ref metaDb, startCursor, endId);

        metaDb.SetLength(startCursor, elements);
        metaDb.SetNumberOfRows(startCursor, rows);
        metaDb.SetLength(endId, elements);
        metaDb.SetNumberOfRows(endId, rows);
    }

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

        metaDb.Append(tokenType, adjustedLocation, adjustedLength, ContainsEscapeSequences(reader));
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
            ContainsScientificNotation(reader.ValueSpan));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Properties, int Rows) CalculateObjectPropertyCount(
        ref MetaDb metaDb,
        Cursor startId,
        Cursor endId)
    {
        if (startId + 1 == endId)
        {
            return (0, 2);
        }

        var properties = 0;
        var rows = 2;
        var current = startId + 1;

        while (current < endId)
        {
            // Property name (string)
            properties++;
            var row = metaDb.Get(current);
            Debug.Assert(row.TokenType is JsonTokenType.PropertyName);

            current += row.NumberOfRows;
            rows += row.NumberOfRows;

            // Property value (any)
            row = metaDb.Get(current);
            current += row.NumberOfRows;
            rows += row.NumberOfRows;
        }

        return (properties, rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Elements, int Rows) CalculateArrayCounts(
        ref MetaDb metaDb,
        Cursor startId,
        Cursor endId)
    {
        if (startId + 1 == endId)
        {
            return (0, 2);
        }

        var elements = 0;
        var rows = 2;
        var current = startId + 1;

        while (current < endId)
        {
            elements++;
            var row = metaDb.Get(current);
            rows += row.NumberOfRows;
            current += row.NumberOfRows;
        }

        return (elements, rows);
    }

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
