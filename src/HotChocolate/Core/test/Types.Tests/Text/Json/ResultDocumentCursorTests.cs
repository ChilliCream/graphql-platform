using Cursor = HotChocolate.Text.Json.ResultDocument.Cursor;

namespace HotChocolate.Text.Json;

public class ResultDocumentCursorTests
{
    public static TheoryData<int> RampChunks()
    {
        var data = new TheoryData<int>();

        // chunks 0..7 cover the geometric ramp (Size1K..Size128K).
        for (var chunk = 0; chunk <= (int)ChunkSize.Size128K; chunk++)
        {
            data.Add(chunk);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void From_Should_RoundTripChunkAndRow_When_RampChunks(int chunk)
    {
        // arrange
        const int row = 42;

        // act
        var cursor = Cursor.From(chunk, row);

        // assert
        Assert.Equal(chunk, cursor.Chunk);
        Assert.Equal(row, cursor.Row);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void ChunkSizeFor_Should_BeMinChunkAndMaxOrdinal_When_RampChunks(int chunk)
    {
        // arrange
        var expected = (ChunkSize)Math.Min(chunk, (int)ChunkSize.Size128K);

        // act
        var size = Cursor.ChunkSizeFor(chunk);

        // assert
        Assert.Equal(expected, size);
    }

    [Fact]
    public void ChunkSizeFor_Should_StayAt128K_When_BeyondRamp()
    {
        // act
        var size = Cursor.ChunkSizeFor(1000);

        // assert
        Assert.Equal(ChunkSize.Size128K, size);
    }

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void RowsPerChunkFor_Should_FollowGeometricSchedule_When_RampChunks(int chunk)
    {
        // arrange
        var expected = (1 << (10 + Math.Min(chunk, (int)ChunkSize.Size128K))) / 20;

        // act
        var rowsPerChunk = Cursor.RowsPerChunkFor(chunk);

        // assert
        Assert.Equal(expected, rowsPerChunk);
    }

    [Fact]
    public void ChunkSize_Should_BeDerivedFromChunkIndex_When_NotStored()
    {
        // arrange
        // row 0 across the ramp, asserting the size is read back from the chunk index alone.
        var first = Cursor.From(0, 0);
        var third = Cursor.From(2, 0);
        var capped = Cursor.From(9, 0);

        // assert
        Assert.Equal(ChunkSize.Size1K, first.ChunkSize);
        Assert.Equal(ChunkSize.Size4K, third.ChunkSize);
        Assert.Equal(ChunkSize.Size128K, capped.ChunkSize);
    }

    [Fact]
    public void Index_Should_BeCumulativeRampPrefixPlusRow_When_WithinRamp()
    {
        // arrange
        // cumulative rows before chunk 3 = R(0)+R(1)+R(2) = 51 + 102 + 204 = 357.
        var cursor = Cursor.From(3, 9);

        // act
        var index = cursor.Index;

        // assert
        Assert.Equal(357 + 9, index);
    }

    [Fact]
    public void Index_Should_BeClosedFormTail_When_BeyondRamp()
    {
        // arrange
        // rows before chunk 7 = 6499; chunk 8 onward each hold 6553 rows.
        var beforeTail = Cursor.From(7, 0);
        var firstTail = Cursor.From(8, 0);

        // assert
        Assert.Equal(6499, beforeTail.Index);
        Assert.Equal(6499 + 6553, firstTail.Index);
    }

    [Fact]
    public void AddRows_Should_CarryAcrossGeometricBoundary_When_RowExceedsRowsPerChunk()
    {
        // arrange
        // chunk 0 holds 51 rows (Size1K); advancing past it lands in chunk 1 (Size2K).
        var cursor = Cursor.From(0, 0);
        var rowsPerChunk = Cursor.RowsPerChunkFor(0);

        // act
        var advanced = cursor.AddRows(rowsPerChunk + 5);

        // assert
        Assert.Equal(1, advanced.Chunk);
        Assert.Equal(5, advanced.Row);
    }

    [Fact]
    public void AddRows_Should_CarryAcrossMultipleGeometricBoundaries_When_DeltaIsLarge()
    {
        // arrange
        // R(0)+R(1) = 51 + 102 = 153 rows fill chunks 0 and 1, landing at chunk 2, row 0.
        var cursor = Cursor.From(0, 0);

        // act
        var advanced = cursor.AddRows(Cursor.RowsPerChunkFor(0) + Cursor.RowsPerChunkFor(1));

        // assert
        Assert.Equal(2, advanced.Chunk);
        Assert.Equal(0, advanced.Row);
    }

    [Fact]
    public void AddRows_Should_BorrowAcrossGeometricBoundary_When_RowGoesNegative()
    {
        // arrange
        // stepping back from chunk 2 row 3 by 10 borrows into chunk 1 (which holds 102 rows).
        var cursor = Cursor.From(2, 3);

        // act
        var moved = cursor.AddRows(-10);

        // assert
        Assert.Equal(1, moved.Chunk);
        Assert.Equal(Cursor.RowsPerChunkFor(1) - 7, moved.Row);
    }

    [Fact]
    public void AddRows_Should_BorrowAcrossMultipleGeometricBoundaries_When_NegativeDeltaSpansChunks()
    {
        // arrange
        // stepping back from chunk 3 row 5 past all of chunks 2 and 1 lands in chunk 0.
        var cursor = Cursor.From(3, 5);
        var delta = 5 + Cursor.RowsPerChunkFor(2) + Cursor.RowsPerChunkFor(1) + 10;

        // act
        var moved = cursor.AddRows(-delta);

        // assert
        Assert.Equal(0, moved.Chunk);
        Assert.Equal(Cursor.RowsPerChunkFor(0) - 10, moved.Row);
        Assert.Equal(cursor.Index - delta, moved.Index);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void AddRows_Should_PreserveIndexContiguity_When_SteppingBackOverBoundary(int chunk)
    {
        // arrange
        var firstInChunk = Cursor.From(chunk, 0);

        // act
        var previous = firstInChunk.AddRows(-1);

        // assert
        Assert.Equal(chunk - 1, previous.Chunk);
        Assert.Equal(Cursor.RowsPerChunkFor(chunk - 1) - 1, previous.Row);
        Assert.Equal(firstInChunk.Index - 1, previous.Index);
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

    [Fact]
    public void AddRows_Should_PreserveIndexContiguity_When_SteppingOverBoundary()
    {
        // arrange
        // last row of chunk 0 then one more step rolls into chunk 1 with a contiguous index.
        var lastInChunk0 = Cursor.From(0, Cursor.RowsPerChunkFor(0) - 1);

        // act
        var next = lastInChunk0.AddRows(1);

        // assert
        Assert.Equal(1, next.Chunk);
        Assert.Equal(0, next.Row);
        Assert.Equal(lastInChunk0.Index + 1, next.Index);
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
        Assert.Equal(ChunkSize.Size1K, zero.ChunkSize);
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

    [Theory]
    [MemberData(nameof(RampChunks))]
    public void Cursor_Should_RoundTripThroughValue_When_RebuiltFromPackedInt(int chunk)
    {
        // arrange
        var original = Cursor.From(chunk, 22);

        // act
        var rebuilt = new Cursor(original.Value);

        // assert
        Assert.Equal(original, rebuilt);
        Assert.Equal(original.Chunk, rebuilt.Chunk);
        Assert.Equal(original.Row, rebuilt.Row);
        Assert.Equal(original.ChunkSize, rebuilt.ChunkSize);
    }

    [Fact]
    public void Value_Should_BeNonNegative_When_MaxChunkAndRowAreStamped()
    {
        // arrange
        const int maxChunk = Cursor.MaxChunks - 1;
        var maxRow = Cursor.RowsPerChunkFor(maxChunk) - 1;

        // act
        var value = Cursor.From(maxChunk, maxRow).Value;

        // assert
        Assert.True(value >= 0);
    }

    [Fact]
    public void Order_Should_BeLinear_When_ComparedByRawValue()
    {
        // arrange
        var earlier = Cursor.From(2, 5);
        var laterRow = Cursor.From(2, 6);
        var laterChunk = Cursor.From(3, 0);

        // assert
        Assert.True(earlier < laterRow);
        Assert.True(laterRow < laterChunk);
        Assert.True(earlier.Index < laterRow.Index);
        Assert.True(laterRow.Index < laterChunk.Index);
    }

    [Fact]
    public void Parent_Should_RoundTripStoredCursorValue_When_ReadBackFromRow()
    {
        // arrange
        var parent = Cursor.From(5, 321);
        var row = new ResultDocument.DbRow(
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
        var row = new ResultDocument.DbRow(
            ElementTokenType.Reference,
            location: target.Value);

        // act
        var rebuilt = new Cursor(row.Location);

        // assert
        Assert.Equal(target, rebuilt);
    }

    [Fact]
    public void OperationReferenceType_Should_NotCollideWithFlagsOrId_When_AllPackedTogether()
    {
        // arrange
        var row = new ResultDocument.DbRow(
            ElementTokenType.PropertyName,
            location: 0,
            operationReferenceId: 0x7FFF,
            operationReferenceType: ResultDocument.OperationReferenceType.Selection,
            flags: (ResultDocument.ElementFlags)0x1FF);

        // assert
        Assert.Equal(0x7FFF, row.OperationReferenceId);
        Assert.Equal((ResultDocument.ElementFlags)0x1FF, row.Flags);
        Assert.Equal(ResultDocument.OperationReferenceType.Selection, row.OperationReferenceType);
    }
}
