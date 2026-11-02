using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Language;

public class JsonValueParserTests
{
    [Fact]
    public void Parse_Should_AbandonCreatedBuilder_When_ParsingFailsWithDoNotSeal()
    {
        // arrange
        // the number creates the builder, the string with a lone continuation byte fails to decode.
        byte[] json = [.. "[1, \""u8, 0x80, .. "\"]"u8];
        using var document = JsonDocument.Parse(json);
        var parser = new JsonValueParser(doNotSeal: true);
        Exception? exception = null;

        // act
        try
        {
            parser.Parse(document.RootElement);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Null(parser._memory);
    }

    [Fact]
    public void Parse_Should_KeepProvidedBuilder_When_ParsingFailsWithDoNotSeal()
    {
        // arrange
        byte[] json = [.. "[1, \""u8, 0x80, .. "\"]"u8];
        using var document = JsonDocument.Parse(json);
        var builder = new Utf8MemoryBuilder();
        var parser = new JsonValueParser(doNotSeal: true) { _memory = builder };
        Exception? exception = null;

        // act
        try
        {
            parser.Parse(document.RootElement);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Same(builder, parser._memory);
        builder.Seal();
        Assert.Equal("1"u8.ToArray(), builder.WrittenSpan.ToArray());
    }

    [Fact]
    public void Parse_JsonElement_StringWithEscapedQuotes_IsUnescaped()
    {
        // arrange
        using var document = JsonDocument.Parse(
            """
            "tag:\"type_portable-lamp\""
            """);
        var parser = new JsonValueParser();

        // act
        var value = parser.Parse(document.RootElement);

        // assert
        var stringValue = Assert.IsType<StringValueNode>(value);
        Assert.Equal("tag:\"type_portable-lamp\"", stringValue.Value);
    }

    [InlineData("\"tag:\\\"lamp\\\"\"", "tag:\"lamp\"")]
    [InlineData("\"a\\\\b\"", "a\\b")]
    [InlineData("\"line\\nbreak\\ttab\"", "line\nbreak\ttab")]
    [InlineData("\"caf\\u00e9\"", "café")]
    [InlineData("\"\\ud83d\\ude00\"", "😀")]
    [Theory]
    public void Parse_Should_UnescapeString_When_ParsingSpan(string json, string expected)
    {
        // arrange
        var parser = new JsonValueParser();

        // act
        var value = parser.Parse(Encoding.UTF8.GetBytes(json));

        // assert
        Assert.Equal(expected, Assert.IsType<StringValueNode>(value).Value);
    }

    [Fact]
    public void Parse_Should_UnescapeString_When_ParsingSequenceWithMultipleSegments()
    {
        // arrange
        var json = Encoding.UTF8.GetBytes("\"tag:\\\"lamp\\\"\"");
        var first = new BufferSegment(json.AsMemory(0, 6));
        var last = first.Append(json.AsMemory(6));
        var sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
        var parser = new JsonValueParser();

        // act
        var value = parser.Parse(sequence);

        // assert
        Assert.Equal("tag:\"lamp\"", Assert.IsType<StringValueNode>(value).Value);
    }

    [Fact]
    public void Parse_Should_UnescapeString_When_WritingToExternalBuffer()
    {
        // arrange
        using var buffer = new PooledArrayWriter();
        var parser = new JsonValueParser(buffer);

        // act
        var value = parser.Parse("""{ "a": "x\"y", "b": "plain" }"""u8);

        // assert
        Assert.Collection(
            Assert.IsType<ObjectValueNode>(value).Fields,
            field => Assert.Equal("x\"y", Assert.IsType<StringValueNode>(field.Value).Value),
            field => Assert.Equal("plain", Assert.IsType<StringValueNode>(field.Value).Value));
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public BufferSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        public BufferSegment Append(ReadOnlyMemory<byte> memory)
        {
            var segment = new BufferSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }
}
