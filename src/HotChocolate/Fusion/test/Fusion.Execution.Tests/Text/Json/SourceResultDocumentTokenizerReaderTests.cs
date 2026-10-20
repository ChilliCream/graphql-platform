using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerReaderTests
{
    private const string Rejected = "rejected";

    [Theory]
    [MemberData(nameof(ReaderInputs))]
    public void Parse_Should_MatchUtf8JsonReader_When_ChunkBoundariesMove(string input)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);

        // act & assert
        AssertReaderParity(bytes, GetPaddings(bytes.Length));
    }

    public static TheoryData<string> ReaderInputs()
        =>
        [
            "", " \t\r\n", "{}", "[]", "[{},[],true,false,null]",
            "0", "-0", "12", "-1.5e+10", "1E-2", "0.0", "1e999",
            "-", "+1", "01", "-01", ".1", "1.", "1e", "1e+", "1e-", "1.e2", "NaN", "Infinity",
            "[0,-0,12.5,1e999,123456789012345678901234567890]",
            "true", "false", "null", "tru", "False", "nullx", "true false",
            "\"\"", "\"é日本😀\"", "\"\\\"\\\\\\/\\b\\f\\n\\r\\t\\u00e9\"",
            "\"\\uD800\"", "\"\\uDC00x\"", "\"\\uD83D\\uDE00\"", "\"\\u0000\"",
            "\"\\x\"", "\"\\u\"", "\"\\u123\"", "\"\\u123G\"", "\"unterminated", "\"\\",
            "{\"\":0,\"a\":1,\"a\":2,\"\\u0061\":3}",
            "{ \"a\" \r\n : \t [1,2] , \"b\" : {} }",
            "{\"a\"}", "{\"a\":}", "{a:1}", "{\"a\":1,}", "[1,]", "[,1]", "[1 2]", "[1:2]",
            "[1}", "{\"a\":1]", "[", "[{}", "[1", "[true", "[1,", "{\"a\":",
            "/*c*/{}", "[1/*c*/]", "[//c\n1]", "\uFEFF{}", "[\u00a01]", "[1\f]", "[1\v]",
            "{}garbage", "{}\u0000", "{}[]", "[]garbage", "[]\u0000", "[][]", "1x", "1 2",
            new string('[', 64) + new string(']', 64),
            new string('[', 65) + new string(']', 65),
            "[" + new string('1', 5000) + ",-" + new string('2', 5000) + ".5e+7]",
            new string('1', 5000),
            "[" + new string('1', 5000) + ".e2]",
            "{\"" + new string('k', 4000) + "\\u0061\":\"" + new string('s', 16000) + "\\n\\uD800\"}",
            "[\"" + string.Concat(Enumerable.Repeat("\\u00e9\\n", 1000)) + "\"]",
            "[\"" + new string('s', 16000) + "\\u123",
            "{\"n\":" + new string('1', 3000) + ",\"s\":\"\\u12"
        ];

    [Fact]
    public void Parse_Should_MatchUtf8JsonReader_When_InputContainsAnyByte()
    {
        // arrange
        var inputs = AnyByteInputs();

        // act & assert
        foreach (var input in inputs)
        {
            AssertReaderParity(input, GetPaddings(input.Length));
        }
    }

    private static IEnumerable<byte[]> AnyByteInputs()
    {
        var contexts = new (byte[] Prefix, byte[] Suffix)[]
        {
            ([], "{}"u8.ToArray()),
            ("[\""u8.ToArray(), "\"]"u8.ToArray()),
            ("[\"\\"u8.ToArray(), "\"]"u8.ToArray()),
            ("[\"\\u0"u8.ToArray(), "00\"]"u8.ToArray()),
            ("[0"u8.ToArray(), ",1]"u8.ToArray()),
            ("[true"u8.ToArray(), "]"u8.ToArray()),
            ("{"u8.ToArray(), "\"a\":1}"u8.ToArray()),
            ("{\""u8.ToArray(), "\":1}"u8.ToArray()),
            ("{\"a\""u8.ToArray(), ":1}"u8.ToArray())
        };

        foreach (var (prefix, suffix) in contexts)
        {
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                yield return [.. prefix, (byte)value, .. suffix];
            }
        }
    }

    private static void AssertReaderParity(byte[] input, IEnumerable<int> paddings)
    {
        var expected = DescribeReader(new ReadOnlySequence<byte>(input), padding: 0, elements: false);

        foreach (var padding in paddings)
        {
            var padded = new byte[padding + input.Length];
            padded.AsSpan(0, padding).Fill((byte)' ');
            input.CopyTo(padded, padding);

            foreach (var layout in Enum.GetValues<ReaderLayout>())
            {
                var (segments, usedChunks, lastLength) = CreateSegments(padded, layout);
                var sequence = CreateSequence(segments, usedChunks, lastLength);
                Assert.Equal(expected, DescribeReader(sequence, padding, elements: false));
                Assert.Equal(expected, DescribeSource(segments, usedChunks, lastLength, padding));
            }
        }
    }

    private static string DescribeReader(ReadOnlySequence<byte> input, int padding, bool elements)
    {
        try
        {
            var reader = new Utf8JsonReader(input);
            var text = new StringBuilder();

            if (elements && (!reader.Read() || reader.TokenType != JsonTokenType.StartArray))
            {
                return Rejected;
            }

            while (reader.Read())
            {
                if (elements && reader.CurrentDepth == 0 && reader.TokenType == JsonTokenType.EndArray)
                {
                    return text.ToString();
                }

                var token = reader.TokenType;
                var length = token is JsonTokenType.String or JsonTokenType.PropertyName
                    ? (int)(reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length) + 2
                    : (int)(reader.BytesConsumed - reader.TokenStartIndex);
                AppendToken(text, token, reader.TokenStartIndex - padding, length, reader.ValueIsEscaped);

                if (elements && reader.CurrentDepth == 1
                    && token is not (JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.PropertyName))
                {
                    text.Append('|');
                }

                // Source documents stop after a root object, as the previous reader-based parser did.
                if (!elements && reader.CurrentDepth == 0 && token == JsonTokenType.EndObject)
                {
                    return text.ToString();
                }
            }

            return elements ? Rejected : text.ToString();
        }
        catch (JsonException)
        {
            return Rejected;
        }
    }

    private static string DescribeSource(MemorySegment[] segments, int usedChunks, int lastLength, int padding)
    {
        using var arena = new MemoryArena();

        try
        {
            using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength);
            var text = new StringBuilder();
            AppendDocument(text, document, padding);
            return text.ToString();
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

    private static void AppendDocument(StringBuilder text, SourceResultDocument document, int padding)
    {
        var cursor = SourceResultDocument.Cursor.Zero;
        var rows = document.GetDbRow(cursor).NumberOfRows;

        for (var i = 0; i < rows; i++, cursor++)
        {
            var row = document.GetDbRow(cursor);
            var container = row.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray
                or JsonTokenType.EndObject or JsonTokenType.EndArray;
            AppendToken(
                text,
                row.TokenType,
                SourceResultDocument.PackedToLinear(row.Location) - padding,
                container ? 1 : row.SizeOrLength,
                !container && row.HasComplexChildren);
        }
    }

    private static void AppendToken(StringBuilder text, JsonTokenType type, long position, int length, bool escaped)
        => text.Append(type).Append('@').Append(position).Append(':').Append(length).Append(':').Append(escaped).Append(';');

    private static IEnumerable<int> GetPaddings(int length)
    {
        yield return 0;
        yield return 1100;

        for (var position = 0; position <= Math.Min(length, 1024); position++)
        {
            if (length <= 128 || position <= 8 || position % 127 == 0)
            {
                yield return 1024 - position;
            }
        }
    }

    private static (MemorySegment[] Segments, int UsedChunks, int LastLength) CreateSegments(byte[] input, ReaderLayout layout)
    {
        var segments = new MemorySegment[64];
        var backing = new byte[input.Length * 2 + 4096];
        var used = 0;
        var offset = 0;
        byte[] pair = [];
        int lastLength;

        do
        {
            var size = SourceResultDocument.GetDataChunkSize(used);
            MemorySegment segment;

            switch (layout)
            {
                case ReaderLayout.Adjacent:
                    segment = new MemorySegment(backing, offset, size);
                    break;

                case ReaderLayout.Separate:
                    segment = new MemorySegment(new byte[size], 0, size);
                    break;

                default:
                    if (used % 2 == 0)
                    {
                        pair = new byte[size + SourceResultDocument.GetDataChunkSize(used + 1)];
                    }

                    segment = new MemorySegment(pair, used % 2 == 0 ? 0 : pair.Length - size, size);
                    break;
            }
            lastLength = Math.Min(size, input.Length - offset);
            input.AsSpan(offset, lastLength).CopyTo(segment.Span);
            segments[used++] = segment;
            offset += lastLength;
        } while (offset < input.Length);

        return (segments, used, lastLength);
    }

    private static ReadOnlySequence<byte> CreateSequence(MemorySegment[] segments, int usedChunks, int lastLength)
    {
        var first = new ReaderSegment(segments[0].Memory[..(usedChunks == 1 ? lastLength : segments[0].Length)]);
        var last = first;

        for (var i = 1; i < usedChunks; i++)
        {
            last = last.Append(segments[i].Memory[..(i == usedChunks - 1 ? lastLength : segments[i].Length)]);
        }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private enum ReaderLayout
    {
        Adjacent,
        Separate,
        Paired
    }

    private sealed class ReaderSegment : ReadOnlySequenceSegment<byte>
    {
        public ReaderSegment(ReadOnlyMemory<byte> memory) => Memory = memory;

        public ReaderSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ReaderSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }
}
