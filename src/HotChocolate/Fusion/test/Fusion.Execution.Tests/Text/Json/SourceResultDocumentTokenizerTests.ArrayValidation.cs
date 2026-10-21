using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerTests
{
    [Theory]
    [InlineData("", "{}", " \t\r\n")]
    [InlineData("{", "\"a\":1}", " \t\r\n")]
    [InlineData("{\"a\"", ":1}", " \t\r\n")]
    // A closing bracket allows the suffix to complete the batch; trailing input is ignored.
    [InlineData("[0", ",1]", " \t\r\n]")]
    [InlineData("[true", "]", " \t\r\n]")]
    [InlineData("[\"\\", "\"]", "\"\\/bfnrt")]
    [InlineData("[\"\\u0", "00\"]", "0123456789abcdefABCDEF")]
    public void ParseArrayElements_Should_AcceptOnlyAllowedBytes_When_ValidatingToken(
        string prefix,
        string suffix,
        string allowed)
    {
        // arrange
        var expected = Encoding.UTF8.GetBytes(allowed).Order();

        // act
        var accepted = GetAcceptedArrayBytes(prefix, suffix);

        // assert
        Assert.Equal(expected, accepted);
    }

    [Theory]
    [InlineData("[\"", "\"]")]
    [InlineData("{\"", "\":1}")]
    public void ParseArrayElements_Should_AcceptOnlyStringContentBytes_When_StringByteIsUnescaped(
        string prefix,
        string suffix)
    {
        // arrange
        var expected = Enumerable.Range(0x20, 0x100 - 0x20)
            .Select(t => (byte)t)
            .Where(t => t is not ((byte)'"' or (byte)'\\'));

        // act
        var accepted = GetAcceptedArrayBytes(prefix, suffix);

        // assert
        Assert.Equal(expected, accepted);
    }

    [Theory]
    [MemberData(nameof(StitchedArrayInputs))]
    public void ParseArrayElements_Should_ReturnTemporaryBuffers_When_ParsingEnds(string input, bool accepted)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);
        var (segments, usedChunks, lastLength) = CreateSegments(bytes, ChunkLayout.Separate);
        using var arena = new MemoryArena();
        var pool = new TrackingArrayPool();

        // act
        var exception = Record.Exception(() =>
        {
            var documents = SourceResultDocument.ParseFilledArrayElements(arena, segments, usedChunks, lastLength, pool);

            foreach (var document in documents)
            {
                document.Dispose();
            }
        });
        arena.Seal();

        // assert
        if (accepted)
        {
            Assert.Null(exception);
        }
        else
        {
            Assert.IsAssignableFrom<JsonException>(exception);
        }

        Assert.True(pool.RentCount > 0);
        Assert.Equal(pool.RentCount, pool.ReturnCount);
        Assert.Empty(pool.Outstanding);
    }

    public static TheoryData<string, bool> StitchedArrayInputs()
    {
        var number = new string('1', 3000);

        return new TheoryData<string, bool>
        {
            { "[{}," + number + ",[]]", true },
            { "[{\"n\":" + number + "}]trailing", true },
            { "[{}," + number + ",]", false },
            { "[" + number + "x]", false },
            { "[" + number, false },
            { "[" + number + ",\"\\u12", false },
            { "[" + number + ",\"" + new string('s', 16000), false },
            { "[" + number + "," + new string('[', 64) + new string(']', 64) + "]", false }
        };
    }

    private static List<byte> GetAcceptedArrayBytes(string prefix, string suffix)
    {
        var before = Encoding.UTF8.GetBytes("[{}," + prefix);
        var after = Encoding.UTF8.GetBytes(suffix + ",[]]");
        var accepted = new List<byte>();

        for (var value = 0; value <= byte.MaxValue; value++)
        {
            byte[] input = [.. before, (byte)value, .. after];

            if (AssertArrayBoundaryIndependence(input, GetBoundaryPaddings(input.Length)) != Rejected)
            {
                accepted.Add((byte)value);
            }
        }

        return accepted;
    }
}
