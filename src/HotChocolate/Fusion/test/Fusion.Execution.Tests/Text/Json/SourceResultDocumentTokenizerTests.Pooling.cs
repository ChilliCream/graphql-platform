using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public partial class SourceResultDocumentTokenizerTests
{
    [Theory]
    [MemberData(nameof(StitchedInputs))]
    public void Parse_Should_ReturnTemporaryBuffers_When_ParsingEnds(string input, bool accepted)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(input);
        var (segments, usedChunks, lastLength) = CreateSegments(bytes, ChunkLayout.Separate);
        using var arena = new MemoryArena();
        var pool = new TrackingArrayPool();

        // act
        var exception = Record.Exception(() =>
        {
            using var document = SourceResultDocument.ParseFilled(arena, segments, usedChunks, lastLength, pool);
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

    public static TheoryData<string, bool> StitchedInputs()
    {
        var number = new string('1', 3000);

        return new TheoryData<string, bool>
        {
            { "[" + number + "]", true },
            { "{\"n\":" + number + "}trailing", true },
            { "[" + number + ",]", false },
            { "[" + number + "x]", false },
            { "{\"n\":" + number, false },
            { "{\"n\":" + number + ",\"s\":\"\\u12", false },
            { "[" + number + ",\"" + new string('s', 16000), false },
            { "[" + number + "," + new string('[', 64) + new string(']', 64) + "]", false }
        };
    }

    private sealed class TrackingArrayPool : ArrayPool<byte>
    {
        public HashSet<byte[]> Outstanding { get; } = [];

        public int RentCount { get; private set; }

        public int ReturnCount { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            var buffer = new byte[minimumLength];
            Outstanding.Add(buffer);
            RentCount++;
            return buffer;
        }

        public override void Return(byte[] array, bool clearArray)
        {
            if (clearArray)
            {
                array.AsSpan().Clear();
            }

            Outstanding.Remove(array);
            ReturnCount++;
        }
    }
}
