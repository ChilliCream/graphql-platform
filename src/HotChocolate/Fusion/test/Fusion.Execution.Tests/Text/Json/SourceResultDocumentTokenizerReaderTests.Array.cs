using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerReaderTests
{
    [Fact]
    public void ParseArrayElements_Should_MatchUtf8JsonReader_When_InputContainsAnyByte()
    {
        // arrange
        var inputs = AnyByteInputs();

        // act & assert
        foreach (var input in inputs)
        {
            byte[] array = [.. "[{},"u8, .. input, .. ",[]]"u8];
            AssertArrayReaderParity(array, GetPaddings(array.Length));
        }
    }

    [Theory]
    [MemberData(nameof(ReaderInputs))]
    public void ParseArrayElements_Should_MatchUtf8JsonReader_When_ChunkBoundariesMove(string input)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes("[{}," + input + ",[]]");

        // act & assert
        AssertArrayReaderParity(bytes, GetPaddings(bytes.Length));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{},1,\"s\",true,false,null,[],{}] trailing")]
    [InlineData("[1]/**/")]
    [InlineData("[1]\u0000")]
    [InlineData("[1]]")]
    [InlineData("[1] [2")]
    [InlineData("[{},{} {}]")]
    [InlineData("[1,]")]
    [InlineData("[")]
    public void ParseArrayElements_Should_MatchUtf8JsonReader_When_ArrayEnds(string input)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);

        // act & assert
        AssertArrayReaderParity(bytes, GetPaddings(bytes.Length));
    }

    [Theory]
    [InlineData(63)]
    [InlineData(64)]
    public void ParseArrayElements_Should_MatchUtf8JsonReader_When_OuterArrayCountsTowardsDepth(int depth)
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("[{}," + new string('[', depth) + new string(']', depth) + "]");

        // act & assert
        AssertArrayReaderParity(input, GetPaddings(input.Length));
    }

    [Theory]
    [MemberData(nameof(StitchedArrayInputs))]
    public void ParseArrayElements_Should_ReturnStitchBuffers_When_ParsingEnds(string input, bool accepted)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);
        var (segments, usedChunks, lastLength) = CreateSegments(bytes, ReaderLayout.Separate);
        using var listener = new BytePoolListener();

        // act
        listener.Start();
        var result = DescribeSourceArray(segments, usedChunks, lastLength, padding: 0);
        listener.Stop();

        // assert
        Assert.Equal(accepted, result != Rejected);
        Assert.True(listener.RentCount > 0);
        Assert.Empty(listener.Outstanding);
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

    private static void AssertArrayReaderParity(byte[] input, IEnumerable<int> paddings)
    {
        var expected = DescribeReader(new ReadOnlySequence<byte>(input), padding: 0, elements: true);

        foreach (var padding in paddings)
        {
            var padded = new byte[padding + input.Length];
            padded.AsSpan(0, padding).Fill((byte)' ');
            input.CopyTo(padded, padding);

            foreach (var layout in Enum.GetValues<ReaderLayout>())
            {
                var (segments, usedChunks, lastLength) = CreateSegments(padded, layout);
                var sequence = CreateSequence(segments, usedChunks, lastLength);
                Assert.Equal(expected, DescribeReader(sequence, padding, elements: true));
                Assert.Equal(expected, DescribeSourceArray(segments, usedChunks, lastLength, padding));
            }
        }
    }

    private static string DescribeSourceArray(MemorySegment[] segments, int usedChunks, int lastLength, int padding)
    {
        using var arena = new MemoryArena();
        List<SourceResultDocument> documents = [];

        try
        {
            documents = SourceResultDocument.ParseFilledArrayElements(arena, segments, usedChunks, lastLength);
            var text = new StringBuilder();

            foreach (var document in documents)
            {
                AppendDocument(text, document, padding);
                text.Append('|');
            }

            return text.ToString();
        }
        catch (JsonException)
        {
            return Rejected;
        }
        finally
        {
            foreach (var document in documents)
            {
                document.Dispose();
            }

            arena.Seal();
        }
    }
}
