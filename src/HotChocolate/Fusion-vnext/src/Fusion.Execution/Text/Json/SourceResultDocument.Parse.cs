using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    private static readonly byte[][] s_emptyObject = ["{}"u8.ToArray()];

    internal static SourceResultDocument CreateEmptyObject()
        => Parse(s_emptyObject, 2, default, pooledMemory: false);

    internal static SourceResultDocument Parse(
        byte[] data,
        int size,
        JsonReaderOptions options = default)
        => Parse([data], size, options);

    internal static SourceResultDocument Parse(
        byte[][] dataChunks,
        int lastLength,
        JsonReaderOptions options = default)
        => Parse(dataChunks, lastLength, options, pooledMemory: true);

    internal static SourceResultDocument Parse(
        byte[][] dataChunks,
        int lastLength,
        JsonReaderOptions options,
        bool pooledMemory)
    {
        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < dataChunks.Length; i++)
        {
            var chunk = dataChunks[i];
            var chunkDataLength = (i == dataChunks.Length - 1) ? lastLength : JsonMemory.ChunkSize;
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
        var reader = new Utf8JsonReader(sequence, options);

        var totalBytes = CalculateTotalBytes(dataChunks, lastLength);
        var estimatedTokens = Math.Max(totalBytes / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(estimatedTokens);

        Span<int> containerStartIndexes = stackalloc int[64];
        var stackIndex = 0;

        try
        {
            while (reader.Read())
            {
                var tokenType = reader.TokenType;
                var location = (int)reader.TokenStartIndex;
                var tokenLength = CalculateTokenLength(reader, tokenType);

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        var objectIndex = metaDb.Append(tokenType, location);
                        containerStartIndexes[stackIndex++] = objectIndex;
                        break;

                    case JsonTokenType.EndObject:
                        CloseObject(ref metaDb, containerStartIndexes[--stackIndex], location);
                        break;

                    case JsonTokenType.StartArray:
                        var arrayIndex = metaDb.Append(tokenType, location);
                        containerStartIndexes[stackIndex++] = arrayIndex;
                        break;

                    case JsonTokenType.EndArray:
                        CloseArray(ref metaDb, containerStartIndexes[--stackIndex], location);
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
        }
        catch
        {
            metaDb.Dispose();
            throw;
        }

        Debug.Assert(stackIndex == 0, "Unclosed containers remain");
        return new SourceResultDocument(metaDb, dataChunks, pooledMemory);
    }

    private static int CalculateTokenLength(Utf8JsonReader reader, JsonTokenType tokenType)
    {
        var totalLength = (int)(reader.BytesConsumed - reader.TokenStartIndex);

        if (tokenType == JsonTokenType.PropertyName)
        {
            // Remove the colon
            return totalLength - 1;
        }

        return totalLength;
    }

    private static void CloseObject(ref MetaDb metaDb, int startIndex, int location)
    {
        var startRow = metaDb.Get(startIndex);

        if (startRow.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected {JsonTokenType.StartObject} but found {startRow.TokenType}.");
        }

        var endIndex = metaDb.Append(JsonTokenType.EndObject, location);
        var (properties, rows) = CalculateObjectPropertyCount(ref metaDb, startIndex, endIndex);

        metaDb.SetLength(startIndex, properties);
        metaDb.SetNumberOfRows(startIndex, rows);
        metaDb.SetLength(endIndex, properties);
        metaDb.SetNumberOfRows(endIndex, rows);
    }

    private static void CloseArray(ref MetaDb metaDb, int startIndex, int location)
    {
        var startRow = metaDb.Get(startIndex);

        if (startRow.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException($"Expected {JsonTokenType.StartArray} but found {startRow.TokenType}.");
        }

        var endIndex = metaDb.Append(JsonTokenType.EndArray, location);
        var (elements, rows) = CalculateArrayCounts(ref metaDb, startIndex, endIndex);

        metaDb.SetLength(startIndex, elements);
        metaDb.SetNumberOfRows(startIndex, rows);
        metaDb.SetLength(endIndex, elements);
        metaDb.SetNumberOfRows(endIndex, rows);
    }

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

        metaDb.Append(tokenType, adjustedLocation, adjustedLength);

        if (ContainsEscapeSequences(reader))
        {
            var currentIndex = metaDb.Length - DbRow.Size;
            metaDb.SetHasComplexChildren(currentIndex);
        }
    }

    private static void AppendNumberToken(
        ref MetaDb metaDb,
        int startLocation,
        int tokenLength,
        Utf8JsonReader reader)
    {
        metaDb.Append(JsonTokenType.Number, startLocation, tokenLength);

        // Check if number uses scientific notation
        if (ContainsScientificNotation(reader.ValueSpan))
        {
            var currentIndex = metaDb.Length - DbRow.Size;

            // Use ComplexChildren flag for "scientific notation"
            metaDb.SetHasComplexChildren(currentIndex);
        }
    }

    private static (int Properties, int Rows) CalculateObjectPropertyCount(ref MetaDb metaDb, int startIndex, int endIndex)
    {
        if (startIndex + 1 == endIndex)
        {
            return (0, 2);
        }

        var properties = 0;
        var rows = 2;
        var currentIndex = startIndex + DbRow.Size;

        while (currentIndex < endIndex)
        {
            properties++;
            var row = metaDb.Get(currentIndex);
            Debug.Assert(row.TokenType is JsonTokenType.PropertyName);
            currentIndex += row.NumberOfRows * DbRow.Size;
            rows += row.NumberOfRows;

            row = metaDb.Get(currentIndex);
            Debug.Assert(row.TokenType is not JsonTokenType.PropertyName);
            currentIndex += row.NumberOfRows * DbRow.Size;
            rows += row.NumberOfRows;
        }

        return (properties, rows);
    }

    private static (int Elements, int Rows) CalculateArrayCounts(ref MetaDb metaDb, int startIndex, int endIndex)
    {
        if (startIndex + 1 == endIndex)
        {
            return (0, 2);
        }

        var elements = 0;
        var rows = 2;
        var currentIndex = startIndex + DbRow.Size;

        while (currentIndex < endIndex)
        {
            elements++;
            var row = metaDb.Get(currentIndex);
            rows += row.NumberOfRows;
            currentIndex += row.NumberOfRows * DbRow.Size;
        }

        return (elements,  rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsEscapeSequences(Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            foreach (var segment in reader.ValueSequence)
            {
                if (segment.Span.IndexOf((byte)'\\') >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        return reader.ValueSpan.IndexOf((byte)'\\') is not -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsScientificNotation(ReadOnlySpan<byte> value)
        => value.IndexOfAny((byte)'e', (byte)'E') >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CalculateTotalBytes(byte[][] dataChunks, int lastLength)
    {
        if (dataChunks.Length == 0)
        {
            return 0;
        }

        // All full chunks
        var total = (dataChunks.Length - 1) * JsonMemory.ChunkSize;

        // Plus the partial last chunk
        total += lastLength;
        return total;
    }
}
