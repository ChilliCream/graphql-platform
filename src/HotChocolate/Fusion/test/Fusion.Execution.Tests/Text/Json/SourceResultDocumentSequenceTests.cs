using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public class SourceResultDocumentSequenceTests
{
    [Theory]
    [InlineData("adjacent", 1024, false)]
    [InlineData("adjacent", 1024, true)]
    [InlineData("adjacent", 1025, false)]
    [InlineData("adjacent", 3072, true)]
    [InlineData("adjacent", 32768, false)]
    [InlineData("gapped", 1025, false)]
    [InlineData("gapped", 3072, true)]
    [InlineData("gapped", 32768, false)]
    [InlineData("separate", 1025, false)]
    [InlineData("separate", 3072, true)]
    [InlineData("separate", 32768, false)]
    public void ParseFilled_Should_PreserveValues_When_ChunksUseDifferentLayouts(
        string layout,
        int payloadLength,
        bool emptyLast)
    {
        // arrange
        const string prefix = "{\"escaped\\\"name\":\"";
        const string suffix = "\\n\\u00E9\",\"number\":123456789}";
        var padding = new string('a', payloadLength - prefix.Length - suffix.Length);
        var payload = prefix + padding + suffix;
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(payload, layout, emptyLast);

        // act
        using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength);
        arena.Seal();

        // assert
        Assert.Equal(payload, document.Root.GetRawText());
        Assert.Equal(padding + "\né", document.Root.GetProperty("escaped\"name").GetString());
        Assert.Equal(123456789, document.Root.GetProperty("number").GetInt32());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1500)]
    public void Parse_Should_ReadRootArray_When_SequenceDoesNotFillLastChunk(int length)
    {
        // arrange
        var payload = "[\"" + new string('a', length) + "\"]";
        var input = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));
        using var arena = new MemoryArena();

        // act
        using var document = SourceResultDocument.Parse(arena, input);
        arena.Seal();

        // assert
        Assert.Equal(payload, document.Root.GetRawText());
    }

    [Theory]
    [InlineData("adjacent")]
    [InlineData("gapped")]
    [InlineData("separate")]
    public void ParseFilledElement_Should_PreserveOffsets_When_ReaderContainsMultipleResults(string layout)
    {
        // arrange
        var firstPayload = "{\"value\":\"" + new string('a', 1500) + "\"}";
        var secondPayload = "{\"value\":\"" + new string('b', 2000) + "\"}";
        var payload = "[" + firstPayload + "," + secondPayload + "]";
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(payload, layout, emptyLast: false);
        var reader = SourceResultDocument.CreateFilledReader(segments, usedChunks, lastLength);
        reader.Read();
        reader.Read();

        // act
        using var first = SourceResultDocument.ParseFilledElement(arena, ref reader, segments, usedChunks);
        reader.Read();
        using var second = SourceResultDocument.ParseFilledElement(arena, ref reader, segments, usedChunks);
        arena.Seal();

        // assert
        Assert.Equal(firstPayload, first.Root.GetRawText());
        Assert.Equal(secondPayload, second.Root.GetRawText());
    }

    [Theory]
    [InlineData("adjacent")]
    [InlineData("separate")]
    public void ParseFilled_Should_ReadPropertyNames_When_WhiteSpacePrecedesColon(string layout)
    {
        // arrange
        var payload = "{\"padding\" :\"" + new string('a', 1500) + "\",\"value\"\t\r\n : 42}";
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(payload, layout, emptyLast: false);

        // act
        using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength);
        arena.Seal();

        // assert
        Assert.Equal(42, document.Root.GetProperty("value").GetInt32());
        Assert.Equal(["padding", "value"], document.Root.EnumerateObject().Select(t => t.Name));
    }

    [Theory]
    [InlineData("[null]", new[] { "null" })]
    [InlineData("[[1]]", new[] { "[1]" })]
    [InlineData("[1,{\"a\":1}]", new[] { "1", "{\"a\":1}" })]
    [InlineData("[{\"a\":1},2]", new[] { "{\"a\":1}", "2" })]
    public void ParseFilledElement_Should_ReadEachElement_When_ElementIsNotAnObject(
        string payload,
        string[] expected)
    {
        // arrange
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(payload, "adjacent", emptyLast: false);

        // act
        var elements = ReadElements(arena, segments, usedChunks, lastLength);
        arena.Seal();

        // assert
        Assert.Equal(expected, elements);
    }

    private static List<string> ReadElements(
        MemoryArena arena,
        MemorySegment[] segments,
        int usedChunks,
        int lastLength)
    {
        var elements = new List<string>();
        var reader = SourceResultDocument.CreateFilledReader(segments, usedChunks, lastLength);
        reader.Read();

        try
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                using var document = SourceResultDocument.ParseFilledElement(arena, ref reader, segments, usedChunks);
                elements.Add(document.Root.GetRawText());
            }
        }
        catch (Exception ex)
        {
            elements.Add(ex.GetType().Name);
        }

        return elements;
    }

    private static (MemorySegment[] Segments, int UsedChunks, int LastLength) CreateSegments(
        string payload,
        string layout,
        bool emptyLast)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var backing = new byte[128 * 1024];
        var segments = new MemorySegment[64];
        var offset = 17;
        var consumed = 0;
        var usedChunks = 0;
        var lastLength = 0;
        while (consumed < bytes.Length || emptyLast)
        {
            var chunkSize = SourceResultDocument.GetDataChunkSize(usedChunks);
            var segment = layout == "separate"
                ? new MemorySegment(new byte[chunkSize + 3], 3, chunkSize)
                : new MemorySegment(backing, offset, chunkSize);
            segments[usedChunks++] = segment;
            lastLength = Math.Min(chunkSize, bytes.Length - consumed);
            bytes.AsSpan(consumed, lastLength).CopyTo(segment.Span);
            consumed += lastLength;
            offset += chunkSize + (layout == "gapped" ? 3 : 0);
            if (lastLength < chunkSize)
            {
                emptyLast = false;
            }
        }
        return (segments, usedChunks, lastLength);
    }
}
