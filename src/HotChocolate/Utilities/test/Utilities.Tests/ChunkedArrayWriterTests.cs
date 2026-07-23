using HotChocolate.Buffers;

namespace HotChocolate.Utilities;

public class ChunkedArrayWriterTests
{
    [Theory]
    [InlineData(JsonMemory.BufferSize - 20, 40)]
    [InlineData(JsonMemory.BufferSize, 40)]
    [InlineData(JsonMemory.BufferSize - 20, JsonMemory.BufferSize + 40)]
    public void WriteAt_Should_OverwriteAdvancedBytes_When_RangeCrossesChunks(
        int location,
        int patchLength)
    {
        // Arrange
        using var writer = new ChunkedArrayWriter();
        var length = location + patchLength + 17;
        var expected = new byte[length];
        WritePattern(expected, seed: 11);
        expected.CopyTo(writer.GetSpan(length));
        writer.Advance(length);

        var patch = new byte[patchLength];
        WritePattern(patch, seed: 41);
        patch.CopyTo(expected.AsSpan(location));
        var position = writer.Position;

        // Act
        writer.WriteAt(location, patch);
        var actual = new byte[length];
        writer.CopyTo(actual, 0, actual.Length);

        // Assert
        Assert.Equal(position, writer.Position);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WriteAt_Should_AllowEmptyPatch_When_LocationEqualsLength()
    {
        // Arrange
        using var writer = new ChunkedArrayWriter();
        writer.Advance(7);

        // Act
        writer.WriteAt(writer.Length, ReadOnlySpan<byte>.Empty);

        // Assert
        Assert.Equal(7, writer.Position);
    }

    [Fact]
    public void WriteAt_Should_RejectRange_When_RangeWasNotAdvanced()
    {
        // Arrange
        using var writer = new ChunkedArrayWriter();
        writer.Advance(8);

        // Act
        var negative = Record.Exception(() => writer.WriteAt(-1, [1]));
        var pastEnd = Record.Exception(() => writer.WriteAt(9, []));
        var overlapsEnd = Record.Exception(() => writer.WriteAt(7, [1, 2]));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(negative);
        Assert.IsType<ArgumentOutOfRangeException>(pastEnd);
        Assert.IsType<ArgumentException>(overlapsEnd);
    }

    [Fact]
    public void WriteAt_Should_UseCurrentAdvancedRange_When_WriterWasReset()
    {
        // Arrange
        using var writer = new ChunkedArrayWriter();
        writer.Advance(16);
        writer.Reset();

        // Act
        var exception = Record.Exception(() => writer.WriteAt(0, [1]));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void WriteAt_Should_RejectPatch_When_WriterIsDisposed()
    {
        // Arrange
        var writer = new ChunkedArrayWriter();
        writer.Advance(1);
        writer.Dispose();

        // Act
        var exception = Record.Exception(() => writer.WriteAt(0, [1]));

        // Assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

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
