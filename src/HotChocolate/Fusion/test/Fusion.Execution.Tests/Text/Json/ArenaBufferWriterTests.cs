using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public class ArenaBufferWriterTests
{
    [Fact]
    public void GetSpan_Should_ReturnBoundedSpan_When_CurrentChunkHasRemainingSpace()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);

        // Act
        var span = writer.GetSpan();

        // Assert
        Assert.Equal(SourceResultDocument.GetDataChunkSize(0), span.Length);
    }

    [Fact]
    public void GetSpan_Should_RoundTripScratchBytes_When_RequestSpansChunks()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var blob = CreateAsciiPattern(SourceResultDocument.GetDataChunkSize(0) + 512);
        var json = Encoding.UTF8.GetBytes($$"""{"blob":"{{blob}}"}""");

        // Act
        var span = writer.GetSpan(json.Length);
        json.CopyTo(span);
        writer.Advance(json.Length);
        using var document = SourceResultDocument.ParseFilled(
            arena,
            writer.Segments,
            writer.UsedChunks,
            writer.LastLength);

        // Assert
        Assert.Equal(json.Length, span.Length);
        Assert.Equal(blob, document.Root.GetProperty("blob").AssertString());
        Assert.Equal(Encoding.UTF8.GetString(json), document.Root.GetRawText());
    }

    [Fact]
    public void Advance_Should_RentNoSegmentTable_When_WritesFitFirstSegment()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);

        // Act
        Write(writer, new byte[SourceResultDocument.GetDataChunkSize(0)]);

        // Assert
        Assert.Equal(0, arena.RentedTableCount);
    }

    [Fact]
    public void Advance_Should_RentSegmentTable_When_WritesPassFirstSegment()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var firstSegmentSize = SourceResultDocument.GetDataChunkSize(0);
        Write(writer, new byte[firstSegmentSize - 4]);
        var start = writer.Position;

        // Act
        Write(writer, "abcdefghij"u8.ToArray());
        var length = 10;
        string[] pieces =
        [
            Encoding.UTF8.GetString(writer.Read(ref start, ref length)),
            Encoding.UTF8.GetString(writer.Read(ref start, ref length))
        ];

        // Assert
        Assert.Equal(1, arena.RentedTableCount);
        Assert.Equal(["abcd", "efghij"], pieces);
    }

    [Fact]
    public void Reset_Should_ReuseRentedSegments_When_WriterIsRewound()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var bytes = Encoding.UTF8.GetBytes(CreateAsciiPattern(3000));
        Write(writer, bytes);
        var secondSegment = writer.Segments[1];

        // Act
        writer.Reset();
        var usedChunksAfterReset = writer.UsedChunks;
        Write(writer, bytes);

        // Assert
        Assert.Equal(1, usedChunksAfterReset);
        Assert.Same(secondSegment.Buffer, writer.Segments[1].Buffer);
        Assert.Equal(secondSegment.Offset, writer.Segments[1].Offset);
        Assert.Equal(3000, writer.Position);
    }

    [Fact]
    public void Reset_Should_RentSegmentsFromNewArena_When_ArenaIsPassed()
    {
        // Arrange
        using var firstArena = new MemoryArena();
        using var secondArena = new MemoryArena();
        using var writer = new ArenaBufferWriter(firstArena);
        var bytes = Encoding.UTF8.GetBytes(CreateAsciiPattern(3000));
        Write(writer, bytes);
        var firstSegment = writer.Segments[0];
        var secondSegment = writer.Segments[1];

        // Act
        writer.Reset(secondArena);
        Write(writer, bytes);

        // Assert
        Assert.NotSame(firstSegment.Buffer, writer.Segments[0].Buffer);
        Assert.NotSame(secondSegment.Buffer, writer.Segments[1].Buffer);
        Assert.Equal(3000, writer.Position);
        Assert.Equal(2, writer.UsedChunks);
    }

    [Fact]
    public void ResetTo_Should_WriteIntoNextSegment_When_PositionIsAtEndOfFullSegment()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var firstSegmentSize = SourceResultDocument.GetDataChunkSize(0);
        Write(writer, Encoding.UTF8.GetBytes(CreateAsciiPattern(firstSegmentSize)));

        // Act
        writer.ResetTo(firstSegmentSize);
        Write(writer, "z"u8.ToArray());

        // Assert
        Assert.Equal(firstSegmentSize + 1, writer.Position);
        Assert.Equal(2, writer.UsedChunks);
        Assert.Equal((byte)'z', writer.Segments[1].Buffer[writer.Segments[1].Offset]);
    }

    [Fact]
    public void SequenceEqual_Should_CompareWrittenBytes_When_RangeSpansSegments()
    {
        // Arrange
        using var arena = new MemoryArena();
        using var writer = new ArenaBufferWriter(arena);
        var firstSegmentSize = SourceResultDocument.GetDataChunkSize(0);
        Write(writer, new byte[firstSegmentSize - 4]);
        var spanning = writer.Position;
        Write(writer, "abcdefghij"u8.ToArray());
        var equal = writer.Position;
        Write(writer, "abcdefghij"u8.ToArray());
        var different = writer.Position;
        Write(writer, "abcdefghiX"u8.ToArray());

        // Act
        var matchesEqual = writer.SequenceEqual(spanning, equal, 10);
        var matchesDifferent = writer.SequenceEqual(spanning, different, 10);

        // Assert
        Assert.True(matchesEqual);
        Assert.False(matchesDifferent);
    }

    private static void Write(ArenaBufferWriter writer, byte[] bytes)
    {
        bytes.CopyTo(writer.GetSpan(bytes.Length));
        writer.Advance(bytes.Length);
    }

    private static string CreateAsciiPattern(int length)
    {
        var chars = new char[length];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + (i % 26));
        }

        return new string(chars);
    }
}
