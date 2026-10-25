using Cursor = HotChocolate.Fusion.Text.Json.CompositeResultDocument.Cursor;

namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentCursorTests
{
    // The cursor uses a fixed [chunk:13][row:13] split and derives the chunk-size bucket from the
    // chunk index through the geometric schedule (Min(chunk, 7)). These cover chunk 0 (Size1K) up
    // to a constant-size tail chunk (Size128K).
    public static TheoryData<int> RampChunks()
    {
        var data = new TheoryData<int>();

        // One chunk per ramp ordinal plus a tail chunk past the ramp.
        foreach (var size in Enum.GetValues<ChunkSize>())
        {
            data.Add((int)size);
        }

        data.Add(50);
        return data;
    }

    public static TheoryData<int, int> ExpectedChunkCapacities()
        => new()
        {
            { 0, 51 },
            { 1, 102 },
            { 2, 204 },
            { 3, 409 },
            { 4, 819 },
            { 5, 1638 },
            { 6, 3276 },
            { 7, 6553 },
            { 8, 6553 }
        };

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void From_Should_RoundTripChunkAndRow_When_AllRampChunks(int chunk)
    {
        // arrange
        const int row = 42;

        // act
        var cursor = Cursor.From(chunk, row);

        // assert
        Assert.Equal(chunk, cursor.Chunk);
        Assert.Equal(row, cursor.Row);
        Assert.Equal(Cursor.ChunkSizeFor(chunk), cursor.ChunkSize);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void RowsPerChunk_Should_DeriveFromChunkIndex_When_AllRampChunks(int chunk)
    {
        // arrange
        var expectedSize = (int)Cursor.ChunkSizeFor(chunk);
        var expected = (1 << (10 + expectedSize)) / 20;
        var cursor = Cursor.From(chunk, 0);

        // act
        var rowsPerChunk = cursor.RowsPerChunk;

        // assert
        Assert.Equal(expected, rowsPerChunk);
    }

    [Theory]
    [MemberData(nameof(ExpectedChunkCapacities))]
    public void RowsPerChunk_Should_MatchIndependentCapacityOracle_When_ChunkChanges(
        int chunk,
        int expected)
    {
        Assert.Equal(expected, Cursor.RowsPerChunkFor(chunk));
        Assert.Equal(expected, Cursor.From(chunk, 0).RowsPerChunk);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void Index_Should_BeRowsBeforeChunkPlusRow_When_AllRampChunks(int chunk)
    {
        // arrange
        const int row = 9;
        var cursor = Cursor.From(chunk, row);

        // The linear index sums RowsPerChunk over all preceding chunks (geometric ramp) plus row.
        var expected = row;
        for (var i = 0; i < chunk; i++)
        {
            expected += Cursor.RowsPerChunkFor(i);
        }

        // act
        var index = cursor.Index;

        // assert
        Assert.Equal(expected, index);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void AddRows_Should_CarryAcrossChunkBoundary_When_RowExceedsRowsPerChunk(int chunk)
    {
        // arrange
        var cursor = Cursor.From(chunk, 0);
        var rowsPerChunk = cursor.RowsPerChunk;

        // act
        var advanced = cursor.AddRows(rowsPerChunk + 5);

        // assert
        Assert.Equal(chunk + 1, advanced.Chunk);
        Assert.Equal(5, advanced.Row);
    }

    [Theory]
    [InlineData(0, 50, 1, 0)]
    [InlineData(1, 101, 2, 0)]
    [InlineData(6, 3275, 7, 0)]
    [InlineData(7, 6552, 8, 0)]
    public void AddRows_Should_AdvanceToNextChunk_When_LastRowIsIncremented(
        int chunk,
        int row,
        int expectedChunk,
        int expectedRow)
    {
        // act
        var next = Cursor.From(chunk, row).AddRows(1);

        // assert
        Assert.Equal(expectedChunk, next.Chunk);
        Assert.Equal(expectedRow, next.Row);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void AddRows_Should_BorrowAcrossChunkBoundary_When_RowGoesNegative(int chunk)
    {
        // arrange
        // Start in chunk+1 so a negative delta borrows into the (possibly smaller) previous chunk.
        var cursor = Cursor.From(chunk + 1, 3);
        var previousRowsPerChunk = Cursor.RowsPerChunkFor(chunk);

        // act
        var moved = cursor.AddRows(-10);

        // assert
        Assert.Equal(chunk, moved.Chunk);
        Assert.Equal(previousRowsPerChunk - 7, moved.Row);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void AddRows_Should_PreserveIndexContiguity_When_SteppingForward(int chunk)
    {
        // arrange
        var cursor = Cursor.From(chunk, 7);

        // act
        var next = cursor.AddRows(1);

        // assert
        Assert.Equal(cursor.Index + 1, next.Index);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void From_Should_AcceptLastAddressableRow_When_AllRampChunks(int chunk)
    {
        // arrange
        var lastRow = Cursor.RowsPerChunkFor(chunk) - 1;

        // act
        var cursor = Cursor.From(chunk, lastRow);

        // assert
        Assert.Equal(chunk, cursor.Chunk);
        Assert.Equal(lastRow, cursor.Row);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(Cursor.MaxChunks)]
    public void From_Should_Throw_When_ChunkIsOutsideCapacity(int chunk)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.From(chunk, 0));
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void From_Should_Throw_When_RowIsOutsideChunkCapacity(int chunk)
    {
        var row = Cursor.RowsPerChunkFor(chunk);

        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.From(chunk, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.From(chunk, row));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void FromByteOffset_Should_Throw_When_OffsetIsNotANonNegativeRowBoundary(int byteOffset)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.FromByteOffset(0, byteOffset));
    }

    [Fact]
    public void FromByteOffset_Should_Throw_When_OffsetIsOutsideChunkCapacity()
    {
        var byteOffset = Cursor.RowsPerChunkFor(0) * 20;

        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.FromByteOffset(0, byteOffset));
    }

    [Fact]
    public void AddRows_Should_ReturnEnd_When_AdvancingPastLastAddressableRow()
    {
        // arrange
        const int chunk = Cursor.MaxChunks - 1;
        var last = Cursor.From(chunk, Cursor.RowsPerChunkFor(chunk) - 1);

        // act
        var end = last.AddRows(1);

        // assert
        Assert.Equal(Cursor.End, end);
        Assert.Equal(chunk, end.Chunk);
        Assert.Equal(Cursor.RowsPerChunkFor(chunk), end.Row);
        Assert.Equal(last.Index + 1, end.Index);
    }

    [Fact]
    public void From_Should_Throw_When_CreatingEndSentinel()
    {
        const int chunk = Cursor.MaxChunks - 1;
        var row = Cursor.RowsPerChunkFor(chunk);

        Assert.Throws<ArgumentOutOfRangeException>(() => Cursor.From(chunk, row));
    }

    [Fact]
    public void AddRows_Should_ReturnLastAddressableRow_When_RewindingFromEnd()
    {
        // act
        var last = Cursor.End.AddRows(-1);

        // assert
        Assert.Equal(Cursor.MaxChunks - 1, last.Chunk);
        Assert.Equal(Cursor.RowsPerChunkFor(last.Chunk) - 1, last.Row);
    }

    [Fact]
    public void AddRows_Should_Throw_When_MovementExceedsCapacity()
    {
        Assert.Throws<OverflowException>(() => Cursor.CreateZero().AddRows(-1));
        Assert.Throws<OverflowException>(() => Cursor.End.AddRows(1));
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void AddRows_Should_Throw_When_DeltaExceedsCapacity(int delta)
    {
        Assert.Throws<OverflowException>(() => Cursor.CreateZero().AddRows(delta));
    }

    [Fact]
    public void IsZero_Should_BeTrue_When_ChunkAndRowAreZero()
    {
        // arrange
        var zero = Cursor.CreateZero();

        // act
        var isZero = zero.IsZero;

        // assert
        Assert.True(isZero);
        Assert.Equal(0, zero.Chunk);
        Assert.Equal(0, zero.Row);
    }

    [Fact]
    public void IsZero_Should_BeFalse_When_AnyChunkOrRowBitIsSet()
    {
        // act
        var rowSet = Cursor.From(0, 1).IsZero;
        var chunkSet = Cursor.From(1, 0).IsZero;

        // assert
        Assert.False(rowSet);
        Assert.False(chunkSet);
    }

    [Fact]
    public void Cursor_Should_RoundTripThroughValue_When_RebuiltFromPackedInt()
    {
        // arrange
        var original = Cursor.From(11, 2222);

        // act
        var rebuilt = new Cursor(original.Value);

        // assert
        Assert.Equal(original, rebuilt);
        Assert.Equal(original.Chunk, rebuilt.Chunk);
        Assert.Equal(original.Row, rebuilt.Row);
    }

    [Fact]
    public void Value_Should_BeNonNegative_When_MaxChunkAndRowAreStamped()
    {
        // arrange
        const int chunk = Cursor.MaxChunks - 1;
        var row = Cursor.RowsPerChunkFor(chunk) - 1;
        var cursor = Cursor.From(chunk, row);

        // act
        var value = cursor.Value;

        // assert
        Assert.True(value >= 0);
        Assert.Equal(chunk, cursor.Chunk);
        Assert.Equal(row, cursor.Row);
    }

    [Fact]
    public void ByteOffset_Should_BeRowTimesDbRowSize_When_RowIsSet()
    {
        // arrange
        var cursor = Cursor.From(2, 13);

        // act
        var byteOffset = cursor.ByteOffset;

        // assert
        Assert.Equal(13 * 20, byteOffset);
    }

    [Fact]
    public void Parent_Should_RoundTripStoredCursorValue_When_ReadBackFromRow()
    {
        // arrange
        var parent = Cursor.From(5, 321);
        var row = new CompositeResultDocument.DbRow(
            ElementTokenType.StartObject,
            location: 0,
            parentRow: parent.Value);

        // act
        var rebuilt = new Cursor(row.Parent);

        // assert
        Assert.Equal(parent, rebuilt);
    }

    [Fact]
    public void ReferenceCursor_Should_RoundTripStoredCursorValue_When_RowIsReference()
    {
        // arrange
        var target = Cursor.From(9, 4096);
        var row = new CompositeResultDocument.DbRow(
            ElementTokenType.Reference,
            location: target.Value);

        // act
        var rebuilt = new Cursor(row.Location);

        // assert
        Assert.Equal(target, rebuilt);
    }

    [Fact]
    public void TokenType_Should_RoundTrip_When_PackedAlongsideSourceDocumentId()
    {
        // arrange
        var row = new CompositeResultDocument.DbRow(
            ElementTokenType.Number,
            sourceDocumentId: 0x7FFF);

        // assert
        Assert.Equal(ElementTokenType.Number, row.TokenType);
        Assert.Equal(0x7FFF, row.SourceDocumentId);
    }
}
