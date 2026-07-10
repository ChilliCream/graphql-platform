using HotChocolate.Buffers;

namespace HotChocolate.Utilities;

public class ChunkedArrayWriterTests
{
    [Fact]
    public void GetSpan_Should_RoundTripScratchBytes_When_RequestSpansChunksAndScratchGrows()
    {
        // Arrange
        var writer = new ChunkedArrayWriter();
        const int prefixLength = JsonMemory.BufferSize - 16;
        var prefix = writer.GetSpan(prefixLength);
        prefix[..prefixLength].Fill(0x5A);
        writer.Advance(prefixLength);

        const int firstScratchSize = 64;
        var firstScratch = writer.GetSpan(firstScratchSize);
        WritePattern(firstScratch[..firstScratchSize], seed: 17);
        writer.Advance(firstScratchSize);

        var expected = new byte[JsonMemory.BufferSize + 97];
        WritePattern(expected, seed: 29);

        // Act
        var location = writer.Position;
        var secondScratch = writer.GetSpan(expected.Length);
        expected.CopyTo(secondScratch);
        writer.Advance(expected.Length);

        var actual = new byte[expected.Length];
        writer.CopyTo(actual, location, actual.Length);
        var exception = Record.Exception(writer.Dispose);

        // Assert
        Assert.Equal(expected, actual);
        Assert.Null(exception);
    }

    private static void WritePattern(Span<byte> span, int seed)
    {
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = (byte)((seed + (i * 31)) & 0xFF);
        }
    }
}
