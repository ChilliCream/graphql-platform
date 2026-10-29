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
