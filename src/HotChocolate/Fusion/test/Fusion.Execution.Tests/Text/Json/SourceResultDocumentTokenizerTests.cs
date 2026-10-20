using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Compares the source JSON tokenizer with the <see cref="Utf8JsonReader"/> based parser that parses
/// the elements of a batched JSON array.
/// </summary>
public class SourceResultDocumentTokenizerTests
{
    // Leading white space that makes every input span at least two data chunks.
    private const int Padding = 1100;

    private const string Rejected = "rejected";

    private static readonly string[] s_validInputs =
        [
            "{}",
            "[]",
            "{\"a\":1}",
            "[1,2,3]",
            "\"root\"",
            "123",
            "-0",
            "0",
            "-1.5e+10",
            "1E-2",
            "0.0",
            "true",
            "false",
            "null",
            " \t\r\n{ \"a\" : [ 1 , 2 ] , \"b\" : { } , \"c\" : [ ] } \n",
            "{\"a\":\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u00e9\\uD83D\\uDE00\"}",
            "{\"\\u0061\":1,\"a\\\"b\":2}",
            "{\"a\":\"\\uD800\",\"b\":\"\\uDC00x\"}",
            "[\"é\",\"日本\",\"😀\",\"\u007f\"]",
            "{\"a\":1}garbage",
            "{\"a\":1}}",
            "{\"a\":1} [",
            "[[[]]]",
            "[{},{}]",
            "{\"a\":[{\"b\":null,\"c\":[true,false]}],\"d\":{\"e\":{}}}",
            "[1.0e5,-0.5,123456789012345678901234567890,1e999,-0.0e-0]",
            "[\"" + new string('x', 5000) + "\",\"" + new string('y', 17) + "\\n" + new string('z', 40) + "\"]",
            "{\"data\":{\"user\":null},\"errors\":[{\"message\":\"x\",\"path\":[\"user\",0],\"extensions\":{\"code\":\"E\"}}]}",
            "[1] \r\n\t ",
            new string('[', 64) + new string(']', 64),
            Nest(64)
        ];

    public static TheoryData<string> ValidInputs() => new(s_validInputs);

    public static TheoryData<string> InvalidInputs()
        =>
        [
            "",
            "   ",
            "{",
            "[",
            "{\"a\"",
            "{\"a\":",
            "{\"a\":1",
            "{\"a\":1,",
            "{\"a\":1,}",
            "[1,]",
            "[,1]",
            "{,}",
            "{\"a\" 1}",
            "{\"a\":1 \"b\":2}",
            "[1 2]",
            "{a:1}",
            "{'a':1}",
            "['a']",
            "[01]",
            "[-]",
            "[-a]",
            "[1.]",
            "[.5]",
            "[1e]",
            "[1e+]",
            "[1.5E-]",
            "[+1]",
            "[0x10]",
            "[NaN]",
            "[Infinity]",
            "[-Infinity]",
            "[tru]",
            "[True]",
            "[nul]",
            "[nulll]",
            "[truex]",
            "[1x]",
            "[1/]",
            "[\"a\\x\"]",
            "[\"a\\u12\"]",
            "[\"a\\u12G4\"]",
            "[\"a\\",
            "[\"a\\u00",
            "[\"unterminated]",
            "[\"tab\there\"]",
            "[\"a\nb\"]",
            "[\"a\u0000b\"]",
            "// c\n{}",
            "/* c */{}",
            "[1]/**/",
            "[1]x",
            "[1] [2]",
            "1 2",
            "\"a\" \"b\"",
            "truefalse",
            "[1}",
            "{\"a\":1]",
            "]",
            "}",
            ":",
            ",",
            "[1,,2]",
            "{\"a\"::1}",
            "{\"a\":}",
            "{\"a\",1}",
            "\uFEFF{}",
            "\u00e9",
            new string('[', 65) + new string(']', 65),
            Nest(65)
        ];

    [Theory]
    [MemberData(nameof(ValidInputs))]
    public void Parse_Should_MatchReaderParser_When_InputIsValid(string input)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);

        // act
        var rows = AssertParity(bytes);

        // assert
        Assert.NotEqual(Rejected, rows);
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Parse_Should_RejectLikeReaderParser_When_InputIsInvalid(string input)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);

        // act
        var rows = AssertParity(bytes);

        // assert
        Assert.Equal(Rejected, rows);
    }

    [Theory]
    // ["<FF>"]: 0xFF never occurs in UTF-8.
    [InlineData(new byte[] { 0x5B, 0x22, 0xFF, 0x22, 0x5D }, true)]
    // ["<C3>"]: a two-byte lead byte without its continuation byte.
    [InlineData(new byte[] { 0x5B, 0x22, 0xC3, 0x22, 0x5D }, true)]
    // ["<C0 AF>"]: an overlong encoding of '/'.
    [InlineData(new byte[] { 0x5B, 0x22, 0xC0, 0xAF, 0x22, 0x5D }, true)]
    // ["<ED A0 80>"]: the surrogate U+D800 encoded as UTF-8.
    [InlineData(new byte[] { 0x5B, 0x22, 0xED, 0xA0, 0x80, 0x22, 0x5D }, true)]
    // [<FF>]: outside a string 0xFF is not the start of any JSON value.
    [InlineData(new byte[] { 0x5B, 0xFF, 0x5D }, false)]
    // <EF BB BF>{}: the reader does not skip a UTF-8 byte order mark, so 0xEF is an unexpected byte.
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0x7B, 0x7D }, false)]
    public void Parse_Should_MatchReaderParser_When_InputIsNotValidUtf8(byte[] input, bool accepted)
    {
        // act
        var rows = AssertParity(input);

        // assert
        Assert.Equal(accepted, rows != Rejected);
    }

    [Fact]
    public void Parse_Should_MatchReaderParser_When_SubgraphResponsesAreMutated()
    {
        // arrange
        var corpus = CreateCorpus();
        var random = new Random(57);
        var accepted = 0;
        var rejected = 0;

        // act
        for (var i = 0; i < 4000; i++)
        {
            var input = Mutate(random, corpus[random.Next(corpus.Length)]);

            if (AssertParity(input) == Rejected)
            {
                rejected++;
            }
            else
            {
                accepted++;
            }
        }

        // assert
        Assert.InRange(accepted, 400, 3600);
        Assert.InRange(rejected, 400, 3600);
    }

    /// <summary>
    /// Parses <paramref name="input"/> with both parsers and returns the rows they agree on, or
    /// <see cref="Rejected"/> when both reject it.
    /// </summary>
    private static string AssertParity(byte[] input)
    {
        var padded = new byte[Padding + input.Length];
        padded.AsSpan(0, Padding).Fill((byte)' ');
        input.CopyTo(padded, Padding);

        var expected = ParseWithReader(padded);
        var contiguous = ParseFilled(padded, contiguous: true);
        var separate = ParseFilled(padded, contiguous: false);
        var unpadded = ParseUnpadded(input);

        Assert.Equal(expected, contiguous);
        Assert.Equal(expected, separate);
        Assert.Equal(expected, unpadded);
        Assert.Equal(expected != Rejected, ParseWithSingleSegmentReader(input));
        return expected;
    }

    private static string ParseWithReader(byte[] input)
    {
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(input, contiguous: false);

        try
        {
            var reader = SourceResultDocument.CreateFilledReader(segments, usedChunks, lastLength);
            reader.Read();
            using var document = SourceResultDocument.ParseFilledElement(arena, ref reader, segments, usedChunks);

            // Only a root object ends the document, any other root value must be the only value.
            if (document.GetDbRow(SourceResultDocument.Cursor.Zero).TokenType != JsonTokenType.StartObject)
            {
                while (reader.Read())
                {
                }
            }

            return DescribeRows(document, Padding);
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            arena.Seal();
        }
    }

    private static string ParseFilled(byte[] input, bool contiguous)
    {
        using var arena = new MemoryArena();
        var (segments, usedChunks, lastLength) = CreateSegments(input, contiguous);

        try
        {
            using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength);
            return DescribeRows(document, Padding);
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            arena.Seal();
        }
    }

    private static (MemorySegment[] Segments, int UsedChunks, int LastLength) CreateSegments(
        byte[] input,
        bool contiguous)
    {
        var segments = new MemorySegment[64];
        var backing = new byte[(input.Length * 2) + 4096];
        var offset = 0;
        var consumed = 0;
        var usedChunks = 0;
        var lastLength = 0;

        while (consumed < input.Length)
        {
            var chunkSize = SourceResultDocument.GetDataChunkSize(usedChunks);
            var segment = contiguous
                ? new MemorySegment(backing, offset, chunkSize)
                : new MemorySegment(new byte[chunkSize], 0, chunkSize);
            segments[usedChunks++] = segment;
            lastLength = Math.Min(chunkSize, input.Length - consumed);
            input.AsSpan(consumed, lastLength).CopyTo(segment.Span);
            consumed += lastLength;
            offset += chunkSize;
        }

        return (segments, usedChunks, lastLength);
    }

    private static string ParseUnpadded(byte[] input)
    {
        using var arena = new MemoryArena();

        try
        {
            using var document = SourceResultDocument.Parse(arena, input, input.Length);
            return DescribeRows(document, 0);
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            arena.Seal();
        }
    }

    private static bool ParseWithSingleSegmentReader(byte[] input)
    {
        var reader = new Utf8JsonReader(input);

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 0)
                {
                    return true;
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string DescribeRows(SourceResultDocument document, int padding)
    {
        var text = new StringBuilder();
        var cursor = SourceResultDocument.Cursor.Zero;
        var rows = document.GetDbRow(cursor).NumberOfRows;

        for (var i = 0; i < rows; i++)
        {
            var row = document.GetDbRow(cursor);
            text.Append(row.TokenType)
                .Append(' ')
                .Append(SourceResultDocument.PackedToLinear(row.Location) - padding)
                .Append(' ')
                .Append(row.SizeOrLength)
                .Append(' ')
                .Append(row.NumberOfRows)
                .Append(' ')
                .Append(row.HasComplexChildren)
                .Append('\n');
            cursor++;
        }

        return text.ToString();
    }

    private static byte[][] CreateCorpus()
    {
        var corpus = new List<byte[]>();

        foreach (var input in s_validInputs)
        {
            corpus.Add(Encoding.UTF8.GetBytes(input));
        }

        corpus.Add(Encoding.UTF8.GetBytes(CreateReviewsResponse(users: 2, compact: true)));
        corpus.Add(Encoding.UTF8.GetBytes(CreateReviewsResponse(users: 1, compact: false)));
        corpus.Add(
            Encoding.UTF8.GetBytes(
                "{\"requestIndex\":1,\"variableIndex\":3,\"data\":{\"productByUpc\":"
                + "{\"inStock\":true,\"shippingEstimate\":-12.5e-3,\"name\":\"T\\u00e9st \\\"1\\\"\"}}}"));
        return [.. corpus];
    }

    private static string CreateReviewsResponse(int users, bool compact)
    {
        var separator = compact ? "" : "\n  ";
        var text = new StringBuilder("{" + separator + "\"data\":{\"_0__entities\":[");

        for (var u = 0; u < users; u++)
        {
            text.Append(u > 0 ? "," : "").Append(separator).Append("{\"reviews\":[");

            for (var r = 0; r < 2; r++)
            {
                text.Append(r > 0 ? "," : "")
                    .Append("{\"id\":\"").Append(r + 1)
                    .Append("\",\"body\":\"Lorem ipsum dolor sit amet, \\\"consectetur\\\" adipiscing elit.\",")
                    .Append(separator)
                    .Append("\"product\":{\"upc\":\"1\",\"price\":899,\"weight\":1.25,\"inStock\":false,\"x\":null}}");
            }

            text.Append("]}");
        }

        return text.Append("]}}").ToString();
    }

    private static byte[] Mutate(Random random, byte[] input)
    {
        var interesting = "{}[],:\"\\ \t\n\r0123456789-+.eEtrufalsnu/\u007f"u8;
        var bytes = new List<byte>(input);
        var mutations = random.Next(1, 4);

        for (var i = 0; i < mutations; i++)
        {
            var position = bytes.Count == 0 ? 0 : random.Next(bytes.Count);
            var value = random.Next(4) == 0
                ? (byte)random.Next(256)
                : interesting[random.Next(interesting.Length)];

            switch (random.Next(6))
            {
                case 0 when bytes.Count > 0:
                    bytes[position] = value;
                    break;

                case 1:
                    bytes.Insert(position, value);
                    break;

                case 2 when bytes.Count > 0:
                    bytes.RemoveRange(position, Math.Min(random.Next(1, 4), bytes.Count - position));
                    break;

                case 3:
                    bytes.RemoveRange(position, bytes.Count - position);
                    break;

                case 4 when bytes.Count > 0:
                    var length = Math.Min(random.Next(1, 16), bytes.Count - position);
                    bytes.InsertRange(random.Next(bytes.Count), bytes.GetRange(position, length));
                    break;

                default:
                    if (bytes.Count > 1)
                    {
                        var other = random.Next(bytes.Count);
                        (bytes[position], bytes[other]) = (bytes[other], bytes[position]);
                    }

                    break;
            }
        }

        return [.. bytes];
    }

    private static string Nest(int depth)
    {
        var text = new StringBuilder();

        for (var i = 0; i < depth; i++)
        {
            text.Append(i % 2 == 0 ? "{\"a\":" : "[");
        }

        text.Append('1');

        for (var i = depth - 1; i >= 0; i--)
        {
            text.Append(i % 2 == 0 ? "}" : "]");
        }

        return text.ToString();
    }
}
