using System.Text;
using System.Text.Json;
using HotChocolate.Fusion.Transport.Http;

namespace HotChocolate.Fusion.Transport;

public class JsonRootEndScannerTests
{
    private const string TrickyDocument =
        "{\"data\":{\"a\":\"}{][\",\"b\":\"\\\"}\",\"c\":[1,{\"d\":\"\\\\\"},[]],\"e\":\"\\\\\\\"]\"}}";

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(4096)]
    public void Scan_Should_ReturnEndOfRoot_When_DocumentArrivesInFragments(int fragmentLength)
    {
        // arrange
        var document = Encoding.UTF8.GetBytes(TrickyDocument + " \r\n");
        var scanner = default(JsonRootEndScanner);

        // act
        var end = ScanInFragments(ref scanner, document, fragmentLength);

        // assert
        Assert.Equal(TrickyDocument.Length, end);
    }

    [Fact]
    public void Scan_Should_ReturnEndOfRoot_When_EscapesAndBracketsCrossBlockBoundaries()
    {
        // arrange
        var document = CreateLongDocument();
        int[] fragmentLengths = [1, 7, 63, 64, 65, 200, int.MaxValue];
        var expected = new List<int>();
        var actual = new List<int>();

        // act
        for (var offset = 0; offset < 70; offset++)
        {
            var bytes = Encoding.UTF8.GetBytes(new string(' ', offset) + document + "\r\n");

            foreach (var fragmentLength in fragmentLengths)
            {
                var scanner = default(JsonRootEndScanner);
                expected.Add(offset + document.Length);
                actual.Add(ScanInFragments(ref scanner, bytes, fragmentLength));
            }
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Scan_Should_ReturnEndOfRoot_When_LongStringsCrossBlockBoundaries()
    {
        // arrange
        int[] textLengths = [0, 1, 62, 63, 64, 65, 127, 128, 129, 200, 333];
        int[] fragmentLengths = [1, 63, 64, 65, 128, 191, 192, 193, 1000, int.MaxValue];
        var expected = new List<int>();
        var actual = new List<int>();

        // act
        foreach (var textLength in textLengths)
        {
            var document = CreateLongStringDocument(textLength);

            for (var offset = 0; offset < 130; offset++)
            {
                var bytes = Encoding.UTF8.GetBytes(new string(' ', offset) + document + "\r\n");

                foreach (var fragmentLength in fragmentLengths)
                {
                    var scanner = default(JsonRootEndScanner);
                    expected.Add(offset + document.Length);
                    actual.Add(ScanInFragments(ref scanner, bytes, fragmentLength));
                }
            }
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("\u00E4")]
    [InlineData("\u20AC")]
    [InlineData("\uD83D\uDE00")]
    public void Scan_Should_ReturnEndOfRoot_When_StringsHoldMultiByteCharacters(string character)
    {
        // arrange
        var document = CreateLongStringDocument(string.Concat(Enumerable.Repeat(character, 70)));
        var rootLength = Encoding.UTF8.GetByteCount(document);
        int[] fragmentLengths = [1, 63, 64, 65, 200, int.MaxValue];
        var expected = new List<int>();
        var actual = new List<int>();

        // act
        for (var offset = 0; offset < 70; offset++)
        {
            var bytes = Encoding.UTF8.GetBytes(new string(' ', offset) + document + "\r\n");

            foreach (var fragmentLength in fragmentLengths)
            {
                var scanner = default(JsonRootEndScanner);
                expected.Add(offset + rootLength);
                actual.Add(ScanInFragments(ref scanner, bytes, fragmentLength));
            }
        }

        // assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(200)]
    public void Scan_Should_ReturnEndOfRoot_When_DocumentIsSplitAtAnyPosition(int textLength)
    {
        // arrange
        var document = CreateLongStringDocument(textLength);
        var bytes = Encoding.UTF8.GetBytes(document + " \r\n");
        var endsInFirstFragment = new List<int>();
        var expectedEndsInFirstFragment = new List<int>();
        var endsInSecondFragment = new List<int>();
        var expectedEndsInSecondFragment = new List<int>();

        // act
        for (var split = 0; split <= bytes.Length; split++)
        {
            var scanner = default(JsonRootEndScanner);
            var rootEndsInFirstFragment = split >= document.Length;
            expectedEndsInFirstFragment.Add(rootEndsInFirstFragment ? document.Length : -1);
            endsInFirstFragment.Add(scanner.Scan(bytes.AsSpan(0, split)));
            expectedEndsInSecondFragment.Add(rootEndsInFirstFragment ? -1 : document.Length - split);
            endsInSecondFragment.Add(scanner.Scan(bytes.AsSpan(split)));
        }

        // assert
        Assert.Equal(expectedEndsInFirstFragment, endsInFirstFragment);
        Assert.Equal(expectedEndsInSecondFragment, endsInSecondFragment);
    }

    [Theory]
    [MemberData(nameof(BoundaryCaseData))]
    public void Scan_Should_ReturnEndOfRoot_When_DocumentIsSplitAtBoundaryCase(string head, string tail)
    {
        // arrange
        // The padding moves the boundary between head and tail to the end of a block.
        var padding = new string(' ', (64 - (head.Length % 64)) % 64);
        var rootEnd = (head + tail).TrimEnd().Length;
        var twoFragments = default(JsonRootEndScanner);
        var twoFragmentsAtBlockEnd = default(JsonRootEndScanner);
        var oneFragment = default(JsonRootEndScanner);

        // act
        var end = ScanFragments(ref twoFragments, head, tail);
        var endAtBlockEnd = ScanFragments(ref twoFragmentsAtBlockEnd, padding + head, tail);
        var endInOneFragment = ScanFragments(ref oneFragment, padding + head + tail + new string(' ', 64));

        // assert
        Assert.Equal(rootEnd, end);
        Assert.Equal(padding.Length + rootEnd, endAtBlockEnd);
        Assert.Equal(padding.Length + rootEnd, endInOneFragment);
    }

    public static TheoryData<string, string> BoundaryCaseData()
    {
        var text = new string('x', 200);

        return new TheoryData<string, string>
        {
            // inside a string
            { "{\"a\":\"x", "y\"}" },
            // after a backslash that escapes a quote
            { "{\"a\":\"x\\", "\"}]\"}" },
            // after an escaped backslash
            { "{\"a\":\"x\\\\", "\"}" },
            // between the two backslashes of an escaped backslash
            { "{\"a\":\"x\\", "\\\"}" },
            // after an escaped quote
            { "{\"a\":\"x\\\"", "}]\"}" },
            // between nested closing brackets
            { "{\"a\":[{\"b\":[]", "}]}" },
            // before the closing bracket of the root
            { "{\"a\":[{\"b\":[]}]", "}" },
            // at the end of the root
            { "{\"a\":[{\"b\":[]}]}", "" },
            // at the end of the root, before trailing whitespace
            { "{\"a\":1}", " \r\n\t" },
            // inside trailing whitespace
            { "{\"a\":1} \r", "\n\t" },
            // before the root
            { "", "{\"a\":1}" },
            // inside leading whitespace
            { " \r\n", "\t{\"a\":1}" },
            // inside an array root
            { "[1,[2,{\"a\":\"]\"}]", "]" },
            // inside a long string
            { "{\"a\":\"" + text, text + "\"}" },
            // inside a long string, after a backslash that escapes a quote
            { "{\"a\":\"" + text + "\\", "\"}]" + text + "\"}" },
            // inside a long string, after an escaped backslash
            { "{\"a\":\"" + text + "\\\\", "\",\"b\":\"" + text + "\"}" },
            // at the end of a long string
            { "{\"a\":\"" + text + "\"", ",\"b\":[\"" + text + "\"]}" },
            // inside a long run of opening brackets
            { "{\"a\":" + new string('[', 40), new string('[', 20) + new string(']', 60) + "}" },
            // inside a long run of closing brackets
            { "{\"a\":" + new string('[', 60) + new string(']', 20), new string(']', 40) + "}" }
        };
    }

    [Fact]
    public void Scan_Should_ReturnEndOfRoot_When_DataFollowsLongRoot()
    {
        // arrange
        var root = CreateLongStringDocument(200);
        var document = Encoding.UTF8.GetBytes(root + "{\"data\":[1]}" + new string(' ', 100));
        var scanner = default(JsonRootEndScanner);

        // act
        var end = scanner.Scan(document);
        var next = scanner.Scan("{}"u8);

        // assert
        Assert.Equal(root.Length, end);
        Assert.Equal(-1, next);
    }

    [Fact]
    public void Scan_Should_KeepState_When_FragmentIsEmpty()
    {
        // arrange
        var scanner = default(JsonRootEndScanner);

        // act
        int[] ends =
        [
            scanner.Scan([]),
            scanner.Scan("{\"a\":\"}\\"u8),
            scanner.Scan([]),
            scanner.Scan("\"}\""u8),
            scanner.Scan([]),
            scanner.Scan("} "u8),
            scanner.Scan([])
        ];

        // assert
        Assert.Equal([-1, -1, -1, -1, -1, 1, -1], ends);
    }

    [Theory]
    [InlineData("{\"data\":{\"a\":1}")]
    [InlineData("{\"data\":\"}")]
    [InlineData("\"{}\"")]
    [InlineData("}{}")]
    [InlineData("123")]
    [InlineData("")]
    public void Scan_Should_ReturnMinusOne_When_RootDoesNotEnd(string document)
    {
        // arrange
        var scanner = default(JsonRootEndScanner);

        // act
        var end = ScanInFragments(ref scanner, Encoding.UTF8.GetBytes(document), 1);

        // assert
        Assert.Equal(-1, end);
    }

    [Theory]
    [MemberData(nameof(LongDocumentsWithoutRootEndData))]
    public void Scan_Should_ReturnMinusOne_When_LongRootDoesNotEnd(string document)
    {
        // arrange
        var scanner = default(JsonRootEndScanner);

        // act
        var end = scanner.Scan(Encoding.UTF8.GetBytes(document));

        // assert
        Assert.Equal(-1, end);
    }

    [Theory]
    [MemberData(nameof(LongDocumentsWithoutRootEndData))]
    public void Scan_Should_ReturnMinusOne_When_LongRootWithoutEndIsSplitAtAnyPosition(string document)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(document);
        var ends = new List<int>();

        // act
        for (var split = 0; split <= bytes.Length; split++)
        {
            var scanner = default(JsonRootEndScanner);
            ends.Add(scanner.Scan(bytes.AsSpan(0, split)));
            ends.Add(scanner.Scan(bytes.AsSpan(split)));
        }

        // assert
        Assert.Equal(Enumerable.Repeat(-1, 2 * (bytes.Length + 1)), ends);
    }

    public static TheoryData<string> LongDocumentsWithoutRootEndData()
    {
        return new TheoryData<string>
        {
            // unterminated string
            "{\"data\":\"" + new string('a', 150),
            // escaped quote across the first block boundary, so the closing brackets stay inside the string
            "{\"data\":\"" + new string('a', 54) + "\\\"" + new string('a', 10) + "}}" + new string('a', 60),
            // closing bracket without an opening bracket inside the second block
            new string(' ', 70) + "]{}" + new string(' ', 60),
            // nested arrays that close inside the blocks while the root stays open
            "{\"data\":{\"items\":[" + string.Concat(Enumerable.Repeat("[1],", 30)) + "1]}",
            // closing brackets inside a string that is longer than several blocks
            "{\"data\":\"" + new string('a', 300) + "}}" + new string('a', 300),
            // escaped quote at the end of a block inside a long string, so the closing brackets stay inside the string
            "{\"data\":\"" + new string('a', 245) + "\\\"}}" + new string('a', 200),
            // backslash at the end of a block in a long string that escapes the quote at the start of the next block
            "{\"data\":\"" + new string('a', 246) + "\\\"}}" + new string('a', 200),
            // backslash at the end of a block that escapes a letter in front of blocks without quotes
            "{\"data\":\"" + new string('a', 54) + "\\n" + new string('a', 200) + "}}",
            // string root that is longer than several blocks
            "\"" + new string('a', 150) + "{}[]" + new string('a', 150) + "\"",
            // number root
            "1234567890123456789012345678901234567890123456789012345678901234567890"
        };
    }

    [Fact]
    public void Scan_Should_ReturnMinusOne_When_RootHasAlreadyEnded()
    {
        // arrange
        var scanner = default(JsonRootEndScanner);
        var first = scanner.Scan("[]"u8);

        // act
        var second = scanner.Scan("{}"u8);

        // assert
        Assert.Equal(2, first);
        Assert.Equal(-1, second);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData(" \t\r\n", true)]
    [InlineData(" 0\r\n", false)]
    [InlineData("{}", false)]
    public void IsWhitespace_Should_ReportWhetherOnlyWhitespaceFollows(string value, bool expected)
    {
        // act
        var isWhitespace = JsonRootEndScanner.IsWhitespace(Encoding.UTF8.GetBytes(value));

        // assert
        Assert.Equal(expected, isWhitespace);
    }

    private static string CreateLongDocument()
    {
        var builder = new StringBuilder("{\"data\":{\"items\":[");

        for (var i = 0; i < 64; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append("{\"id\":").Append(i).Append(",\"text\":\"");
            builder.Append('\\', 2 * (i % 3));
            builder.Append("\\\"}{][");
            builder.Append('a', i % 17);
            builder.Append("\",\"nested\":[[{\"x\":\"\\\\\"}],[]]}");
        }

        builder.Append("]}}");
        var document = builder.ToString();
        using var validated = JsonDocument.Parse(document);
        return document;
    }

    private static string CreateLongStringDocument(int textLength)
        => CreateLongStringDocument(new string('a', textLength));

    private static string CreateLongStringDocument(string text)
    {
        var builder = new StringBuilder("{\"data\":{\"items\":[");

        // a string with brackets that ends with an escaped quote
        builder.Append("{\"text\":\"").Append(text).Append("}{][\\\"\"},");
        // a string that ends with an escaped backslash
        builder.Append("{\"text\":\"").Append(text).Append("]}\\\\\"},");
        // a string that starts with escapes, behind nested closing brackets
        builder.Append("[[{\"text\":\"\\\\\\\"").Append(text).Append("\"}]],");
        // a string without quotes, backslashes or brackets
        builder.Append('"').Append(text).Append(text).Append("\",");
        // strings with escapes that are followed by text
        builder.Append("\"\\n").Append(text).Append("\\u005D").Append(text).Append("\",");
        builder.Append("\"\\t").Append(text).Append("\",\"\\\\\\t").Append(text).Append("\\\"").Append(text).Append("\",");
        // strings with an escaped backslash in front of text and behind text
        builder.Append("\"\\\\").Append(text).Append("\",\"").Append(text).Append("\\\\").Append(text).Append("\\\\\",");
        // a property name with brackets
        builder.Append("{\"").Append(text).Append("}]\":[]}");

        builder.Append("]}}");
        var document = builder.ToString();
        using var validated = JsonDocument.Parse(document);
        return document;
    }

    private static int ScanFragments(ref JsonRootEndScanner scanner, params string[] fragments)
    {
        var offset = 0;

        foreach (var fragment in fragments)
        {
            var end = scanner.Scan(Encoding.UTF8.GetBytes(fragment));

            if (end >= 0)
            {
                return offset + end;
            }

            offset += fragment.Length;
        }

        return -1;
    }

    private static int ScanInFragments(ref JsonRootEndScanner scanner, byte[] document, int fragmentLength)
    {
        for (var offset = 0; offset < document.Length; offset += fragmentLength)
        {
            var length = Math.Min(fragmentLength, document.Length - offset);
            var end = scanner.Scan(document.AsSpan(offset, length));

            if (end >= 0)
            {
                return offset + end;
            }
        }

        return -1;
    }
}
