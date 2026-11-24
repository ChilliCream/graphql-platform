namespace HotChocolate.Fusion.Text.Json;

public sealed class SourceResultDocumentCursorTests
{
    [Fact]
    public void FirstOfNextChunk_Should_ReturnFirstRowOfLastChunk_When_CursorIsInChunkBeforeLast()
    {
        // arrange
        var cursor = SourceResultDocument.Cursor.From(SourceResultDocument.Cursor.MaxChunks - 2, 7);

        // act
        var next = SourceResultDocument.Cursor.FirstOfNextChunk(cursor);

        // assert
        Assert.Equal(SourceResultDocument.Cursor.MaxChunks - 1, next.Chunk);
        Assert.Equal(0, next.Row);
    }

    [Fact]
    public void FirstOfNextChunk_Should_Throw_When_CursorIsInLastChunk()
    {
        // arrange
        var cursor = SourceResultDocument.Cursor.From(SourceResultDocument.Cursor.MaxChunks - 1, 7);

        // act
        var exception = Record.Exception(() => SourceResultDocument.Cursor.FirstOfNextChunk(cursor));

        // assert
        Assert.Equal(
            "The source result document has exceeded its maximum data capacity.",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void RowsPerChunkFor_Should_AddUpToRowCapacity_When_EveryChunkIsFull()
    {
        // arrange
        var rows = 0L;

        // act
        for (var chunkIndex = 0; chunkIndex < SourceResultDocument.Cursor.MaxChunks; chunkIndex++)
        {
            rows += SourceResultDocument.Cursor.RowsPerChunkFor(chunkIndex);
        }

        // assert
        Assert.Equal(44_608_087L, rows);
    }
}
