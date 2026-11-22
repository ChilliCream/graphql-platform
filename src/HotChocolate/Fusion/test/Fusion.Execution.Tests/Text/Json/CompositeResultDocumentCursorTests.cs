using Cursor = HotChocolate.Fusion.Text.Json.CompositeResultDocument.Cursor;

namespace HotChocolate.Fusion.Text.Json;

public class CompositeResultDocumentCursorTests
{
    // The cursor uses a fixed [chunk:13][row:13] split and derives the chunk-size bucket from the
    // chunk index through the chunk schedule. These cover chunk 0 (Size1K), the first and the last
    // chunk of every later size step, and a constant-size tail chunk (Size128K).
    public static TheoryData<int> RampChunks()
        => [0, 1, 2, 3, 4, 5, 11, 12, 15, 16, 17, 18, 50];

    public static TheoryData<int, int> ExpectedChunkCapacities()
        => new()
        {
            { 0, 51 },
            { 1, 102 },
            { 2, 204 },
            { 3, 409 },
            { 4, 819 },
            { 5, 819 },
            { 11, 819 },
            { 12, 1638 },
            { 15, 1638 },
            { 16, 3276 },
            { 17, 3276 },
            { 18, 6553 },
            { 19, 6553 }
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
    [InlineData(4, 818, 5, 0)]
    [InlineData(11, 818, 12, 0)]
    [InlineData(15, 1637, 16, 0)]
    [InlineData(17, 3275, 18, 0)]
    [InlineData(18, 6552, 19, 0)]
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
        var original = Cursor.From(18, 2222);

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
        var target = Cursor.From(19, 4096);
        var row = new CompositeResultDocument.DbRow(
            ElementTokenType.Reference,
            location: target.Value);

        // act
        var rebuilt = new Cursor(row.Location);

        // assert
        Assert.Equal(target, rebuilt);
    }

    // 0x800 sets the lowest id bit a reader on a shifted layout would mistake for the token type,
    // 0x8000 is the first id a 15 bit field loses, and the maximum sets the sign bit.
    public static TheoryData<int> SourceDocumentIds()
        => new() { 0x800, 0x8000, CompositeResultDocument.DbRow.MaxSourceDocumentId };

    [Theory]
    [MemberData(nameof(SourceDocumentIds))]
    public void TokenType_Should_RoundTrip_When_PackedAlongsideSourceDocumentId(int sourceDocumentId)
    {
        // arrange
        // act
        var row = new CompositeResultDocument.DbRow(
            ElementTokenType.Number,
            sourceDocumentId: sourceDocumentId);

        // assert
        Assert.Equal(ElementTokenType.Number, row.TokenType);
        Assert.Equal(sourceDocumentId, row.SourceDocumentId);
    }
}
