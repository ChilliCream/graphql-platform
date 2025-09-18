using System.Buffers;
using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument
{
    internal static SourceResultDocument Parse(
        byte[][] dataChunks,
        int lastLength,
        JsonReaderOptions options = default)
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

        // Estimate based on JSON size - roughly one token per 12 bytes
        var totalBytes = CalculateTotalBytes(dataChunks, lastLength);
        var estimatedTokens = Math.Max(totalBytes / 12, 100);
        var metaDb = MetaDb.CreateForEstimatedRows(estimatedTokens);

        var containerStack = new Stack<int>(); // Track open containers (StartObject/StartArray)

        try
        {
            while (reader.Read())
            {
                var tokenType = reader.TokenType;
                var startLocation = (int)reader.TokenStartIndex;
                var tokenLength = (int)(reader.BytesConsumed - reader.TokenStartIndex);

                switch (tokenType)
                {
                    case JsonTokenType.StartObject:
                        var objectIndex = AppendContainer(ref metaDb, tokenType, startLocation);
                        containerStack.Push(objectIndex);
                        break;

                    case JsonTokenType.EndObject:
                        CloseContainer(ref metaDb, containerStack, JsonTokenType.StartObject, startLocation, tokenLength);
                        break;

                    case JsonTokenType.StartArray:
                        var arrayIndex = AppendContainer(ref metaDb, tokenType, startLocation);
                        containerStack.Push(arrayIndex);
                        break;

                    case JsonTokenType.EndArray:
                        CloseContainer(ref metaDb, containerStack, JsonTokenType.StartArray, startLocation, tokenLength);
                        break;

                    case JsonTokenType.PropertyName:
                    case JsonTokenType.String:
                        AppendStringToken(ref metaDb, tokenType, startLocation, tokenLength, reader);
                        break;

                    case JsonTokenType.Number:
                        AppendNumberToken(ref metaDb, startLocation, tokenLength, reader);
                        break;

                    case JsonTokenType.True:
                    case JsonTokenType.False:
                    case JsonTokenType.Null:
                        metaDb.Append(tokenType, startLocation, tokenLength);
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

        Debug.Assert(containerStack.Count == 0, "Unclosed containers remain");

        return new SourceResultDocument(metaDb, dataChunks);
    }

    private static int AppendContainer(ref MetaDb metaDb, JsonTokenType tokenType, int startLocation)
    {
        // Containers start with unknown size (-1) until we know their element count
        metaDb.Append(tokenType, startLocation, DbRow.UnknownSize);

        // Return index of the container we just added
        return metaDb.Length - DbRow.Size;
    }

    private static void CloseContainer(ref MetaDb metaDb, Stack<int> containerStack, JsonTokenType expectedStartType,
        int startLocation, int tokenLength)
    {
        if (containerStack.Count == 0)
        {
            throw new JsonException($"Unexpected {expectedStartType.ToString().Replace("Start", "End")} token");
        }

        var containerIndex = containerStack.Pop();
        var containerRow = metaDb.Get(containerIndex);

        if (containerRow.TokenType != expectedStartType)
        {
            throw new JsonException($"Mismatched container types");
        }

        // Add the EndObject/EndArray token
        metaDb.Append(expectedStartType == JsonTokenType.StartObject ? JsonTokenType.EndObject : JsonTokenType.EndArray,
            startLocation, tokenLength);

        // Calculate how many elements are in this container
        var rowsInContainer = (metaDb.Length - containerIndex) / DbRow.Size;
        var elementCount = CalculateElementCount(containerRow.TokenType, rowsInContainer);

        // Update the container with actual element count and row span
        metaDb.SetLength(containerIndex, elementCount);
        metaDb.SetNumberOfRows(containerIndex, rowsInContainer);

        // Check if this container has complex children (nested objects/arrays)
        if (ContainsComplexChildren(ref metaDb, containerIndex, metaDb.Length))
        {
            metaDb.SetHasComplexChildren(containerIndex);
        }
    }

    private static void AppendStringToken(ref MetaDb metaDb, JsonTokenType tokenType, int startLocation, int tokenLength,
        Utf8JsonReader reader)
    {
        metaDb.Append(tokenType, startLocation, tokenLength);

        // Check if the string requires unescaping by comparing the span with the value
        // If they differ, it means there are escape sequences
        // -2 for quotes
        if (reader.ValueSpan.Length != tokenLength - 2 || ContainsEscapeSequences(reader.ValueSpan))
        {
            var currentIndex = metaDb.Length - DbRow.Size;

            // Use ComplexChildren flag for "needs unescaping"
            metaDb.SetHasComplexChildren(currentIndex);
        }
    }

    private static void AppendNumberToken(ref MetaDb metaDb, int startLocation, int tokenLength, Utf8JsonReader reader)
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

    private static int CalculateElementCount(JsonTokenType containerType, int totalRows)
    {
        if (containerType == JsonTokenType.StartObject)
        {
            // For objects: count property name + value pairs, minus start/end tokens
            return (totalRows - 2) / 2; // Each property is PropertyName + Value
        }
        else // StartArray
        {
            // For arrays: count all non-container tokens, minus start/end tokens
            return totalRows - 2; // Simple count for now - could be more sophisticated
        }
    }

    private static bool ContainsComplexChildren(ref MetaDb metaDb, int containerStart, int containerEnd)
    {
        // Scan through container contents looking for nested objects/arrays
        for (var i = containerStart + DbRow.Size; i < containerEnd - DbRow.Size; i += DbRow.Size)
        {
            var row = metaDb.Get(i);
            if (row.TokenType == JsonTokenType.StartObject || row.TokenType == JsonTokenType.StartArray)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsEscapeSequences(ReadOnlySpan<byte> value)
    {
        // Quick scan for backslash escape sequences
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == (byte)'\\')
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsScientificNotation(ReadOnlySpan<byte> value)
    {
        // Check for 'e' or 'E' in the number
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == (byte)'e' || value[i] == (byte)'E')
            {
                return true;
            }
        }

        return false;
    }

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
