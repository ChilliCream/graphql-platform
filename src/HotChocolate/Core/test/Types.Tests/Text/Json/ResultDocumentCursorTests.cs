using Cursor = HotChocolate.Text.Json.ResultDocument.Cursor;

namespace HotChocolate.Text.Json;

public class ResultDocumentCursorTests
{
    public static TheoryData<int> AllChunkSizes()
    {
        var data = new TheoryData<int>();

        foreach (var size in Enum.GetValues<ChunkSize>())
        {
            data.Add((int)size);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void From_Should_RoundTripChunkRowAndSize_When_AllOrdinals(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        const int chunk = 17;
        const int row = 4242;

        // act
        var cursor = Cursor.From(size, chunk, row);

        // assert
        Assert.Equal(size, cursor.ChunkSize);
        Assert.Equal(chunk, cursor.Chunk);
        Assert.Equal(row, cursor.Row);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void RowsPerChunk_Should_DeriveFromOwnSizeBits_When_AllOrdinals(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var expected = (1 << (10 + (int)size)) / 20;
        var cursor = Cursor.From(size, 0, 0);

        // act
        var rowsPerChunk = cursor.RowsPerChunk;

        // assert
        Assert.Equal(expected, rowsPerChunk);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void Index_Should_BeChunkTimesRowsPerChunkPlusRow_When_AllOrdinals(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var cursor = Cursor.From(size, 3, 9);
        var expected = (3 * cursor.RowsPerChunk) + 9;

        // act
        var index = cursor.Index;

        // assert
        Assert.Equal(expected, index);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void AddRows_Should_CarryAcrossChunkBoundary_When_RowExceedsRowsPerChunk(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var cursor = Cursor.From(size, 2, 0);
        var rowsPerChunk = cursor.RowsPerChunk;

        // act
        var advanced = cursor.AddRows(rowsPerChunk + 5);

        // assert
        Assert.Equal(3, advanced.Chunk);
        Assert.Equal(5, advanced.Row);
        Assert.Equal(size, advanced.ChunkSize);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void AddRows_Should_BorrowAcrossChunkBoundary_When_RowGoesNegative(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var cursor = Cursor.From(size, 4, 3);
        var rowsPerChunk = cursor.RowsPerChunk;

        // act
        var moved = cursor.AddRows(-10);

        // assert
        Assert.Equal(3, moved.Chunk);
        Assert.Equal(rowsPerChunk - 7, moved.Row);
        Assert.Equal(size, moved.ChunkSize);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void AddRows_Should_PreserveIndexContiguity_When_SteppingForward(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var cursor = Cursor.From(size, 1, 7);

        // act
        var next = cursor.AddRows(1);

        // assert
        Assert.Equal(cursor.Index + 1, next.Index);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void IsZero_Should_BeTrue_When_ChunkAndRowAreZeroRegardlessOfSize(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var zero = Cursor.CreateZero(size);

        // act
        var isZero = zero.IsZero;

        // assert
        Assert.True(isZero);
        Assert.Equal(size, zero.ChunkSize);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void IsZero_Should_BeFalse_When_AnyChunkOrRowBitIsSet(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;

        // act
        var rowSet = Cursor.From(size, 0, 1).IsZero;
        var chunkSet = Cursor.From(size, 1, 0).IsZero;

        // assert
        Assert.False(rowSet);
        Assert.False(chunkSet);
    }

    [Theory]
    [MemberData(nameof(AllChunkSizes))]
    public void Cursor_Should_RoundTripThroughValue_When_RebuiltFromPackedInt(int sizeOrdinal)
    {
        // arrange
        var size = (ChunkSize)sizeOrdinal;
        var original = Cursor.From(size, 11, 2222);

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
        var cursor = Cursor.From(ChunkSize.Size128K, Cursor.MaxChunks - 1, 16383);

        // act
        var value = cursor.Value;

        // assert
        Assert.True(value >= 0);
    }

    [Fact]
    public void Parent_Should_RoundTripStoredCursorValue_When_ReadBackFromRow()
    {
        // arrange
        var parent = Cursor.From(ChunkSize.Size128K, 5, 321);
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
        var target = Cursor.From(ChunkSize.Size128K, 9, 4096);
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
