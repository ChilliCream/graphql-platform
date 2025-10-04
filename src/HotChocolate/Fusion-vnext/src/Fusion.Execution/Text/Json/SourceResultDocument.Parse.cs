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
        const int chunkSizeBytes = 128 * 1024;

        SequenceSegment? first = null;
        SequenceSegment? previous = null;

        for (var i = 0; i < dataChunks.Length; i++)
        {
            var chunk = dataChunks[i];
            var chunkDataLength = (i == dataChunks.Length - 1) ? lastLength : chunkSizeBytes;
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
                        var objectIndex =
                            AppendContainer(
                                ref metaDb,
                                tokenType,
                                location);
                        containerStartIndexes[stackIndex++] = objectIndex;
                        break;

                    case JsonTokenType.EndObject:
                        CloseContainer(
                            ref metaDb,
                            containerStartIndexes[++stackIndex],
                            location,
                            JsonTokenType.StartObject);
                        break;

                    case JsonTokenType.StartArray:
                        var arrayIndex =
                            AppendContainer(
                                ref metaDb,
                                tokenType,
                                location);
                        containerStartIndexes[stackIndex++] = arrayIndex;
                        containerStartIndexes.Push(arrayIndex);
                        break;

                    case JsonTokenType.EndArray:
                        CloseContainer(
                            ref metaDb,
                            containerStartIndexes,
                            JsonTokenType.StartArray,
                            location,
                            tokenLength);
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

        Debug.Assert(containerStartIndexes.Count == 0, "Unclosed containers remain");
        return new SourceResultDocument(metaDb, dataChunks, true);
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

    private static int AppendContainer(ref MetaDb metaDb, JsonTokenType tokenType, int location)
    {
        // Containers start with unknown size (-1) until we know their element count
        metaDb.Append(tokenType, location, DbRow.UnknownSize);

        // Return index of the container we just added
        return metaDb.Length - DbRow.Size;
    }

    private static void CloseContainer(
        ref MetaDb metaDb,
        int startIndex,
        int location,
        JsonTokenType expectedStartType)
    {
        var startRow = metaDb.Get(startIndex);

        if (startRow.TokenType != expectedStartType)
        {
            throw new JsonException("Mismatched container types");
        }

        // Add the EndObject/EndArray token
        var endTokenType =
            expectedStartType is JsonTokenType.StartObject
                ? JsonTokenType.EndObject
                : JsonTokenType.EndArray;
        metaDb.Append(endTokenType, location, DbRow.UnknownSize);
        var endIndex = metaDb.Length - DbRow.Size;

        // Calculate how many rows are in this container (including start/end tokens)
        var rowsInContainer = (metaDb.Length - containerIndex) / DbRow.Size;
        var elementCount = endTokenType is JsonTokenType.EndObject
            ? CalculateElementCount(startRow.TokenType, rowsInContainer);
            :  CalculateArrayElementCount(ref metaDb, )

        // Update the StartObject/StartArray with actual element count and forward skip count
        metaDb.SetLength(containerIndex, elementCount);
        metaDb.SetNumberOfRows(containerIndex, rowsInContainer);

        // Update the EndObject/EndArray with backward skip count (Microsoft's approach)
        var endTokenIndex = metaDb.Length - DbRow.Size;
        // Skip back to previous element before this container
        var backwardSkipCount = rowsInContainer - 1;
        metaDb.SetNumberOfRows(endTokenIndex, backwardSkipCount);

        // Check if this container has complex children (nested objects/arrays)
        if (ContainsComplexChildren(ref metaDb, containerIndex, metaDb.Length))
        {
            metaDb.SetHasComplexChildren(containerIndex);
        }
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

    private static int CalculateObjectPropertyCount(ref MetaDb metaDb, int startIndex, int endIndex)
    {
        if (startIndex + 1 == endIndex)
        {
            return 0;
        }

        var count = 0;
        var currentIndex = startIndex + DbRow.Size;

        while (currentIndex < endIndex)
        {
            count++;
            var row = metaDb.Get(currentIndex);
            Debug.Assert(row.TokenType is JsonTokenType.PropertyName);
            currentIndex += row.NumberOfRows * DbRow.Size;

            row = metaDb.Get(currentIndex);
            Debug.Assert(row.TokenType is not JsonTokenType.PropertyName);
            currentIndex += row.NumberOfRows * DbRow.Size;
        }

        return count;
    }

    private static int CalculateArrayElementCount(ref MetaDb metaDb, int startIndex, int endIndex)
    {
        if (startIndex + 1 == endIndex)
        {
            return 0;
        }

        var count = 0;
        var currentIndex = startIndex + DbRow.Size;

        while (currentIndex < endIndex)
        {
            count++;
            var row = metaDb.Get(currentIndex);
            currentIndex += row.NumberOfRows * DbRow.Size;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsComplexChildren(ref MetaDb metaDb, int containerStart, int containerEnd)
    {
        // Scan through container contents looking for nested objects/arrays
        for (var i = containerStart + DbRow.Size; i < containerEnd - DbRow.Size; i += DbRow.Size)
        {
            var row = metaDb.Get(i);
            if (row.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                return true;
            }
        }

        return false;
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
        var total = (dataChunks.Length - 1) * 131072;

        // Plus the partial last chunk
        total += lastLength;
        return total;
    }
}
