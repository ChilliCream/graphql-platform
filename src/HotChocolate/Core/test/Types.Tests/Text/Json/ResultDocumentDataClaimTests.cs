namespace HotChocolate.Text.Json;

public class ResultDocumentDataClaimTests
{
    // The data location packs the chunk index in the high 12 bits (4096 chunks max) and the byte
    // offset within the chunk in the low 17 bits. Chunk byte sizes follow the geometric schedule:
    // chunk 0 holds 1024 bytes, chunk 1 holds 2048, doubling up to 131072 from chunk 7 onward.
    private const int LastChunk = 4095;
    private const int LargestChunkBytes = 131_072;

    private static long Head(int chunk, int offset) => ((long)chunk << 32) | (uint)offset;

    [Fact]
    public void ComputeDataClaim_Should_Throw_When_DataCapacityExceeded()
    {
        // arrange
        // the head sits near the end of the last chunk, so the value spills into chunk 4096.
        var head = Head(LastChunk, LargestChunkBytes - 100);

        // act
        void Act() => ResultDocument.ComputeDataClaim(head, 200);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void ComputeDataClaim_Should_Succeed_When_ClaimFillsLastChunkExactly()
    {
        // arrange
        var head = Head(LastChunk, 0);

        // act
        var (next, location, lastChunk) = ResultDocument.ComputeDataClaim(head, LargestChunkBytes);

        // assert
        Assert.Equal(LastChunk, lastChunk);
        Assert.Equal(LastChunk << 17, location);
        Assert.Equal(Head(LastChunk + 1, 0), next);
    }

    [Fact]
    public void ComputeDataClaim_Should_SpanIntoNextChunkWithoutGap_When_ValueDoesNotFitCurrentChunk()
    {
        // arrange
        // a 2000-byte value at offset 100 of chunk 0 (1024 bytes) starts exactly at the head
        // and spans into chunk 1: 924 bytes fill chunk 0 and 1076 land in chunk 1.
        var head = Head(0, 100);

        // act
        var (next, location, lastChunk) = ResultDocument.ComputeDataClaim(head, 2000);

        // assert
        Assert.Equal(100, location);
        Assert.Equal(1, lastChunk);
        Assert.Equal(Head(1, 1076), next);
    }

    [Fact]
    public void ComputeDataClaim_Should_SpanRampChunks_When_ValueExceedsSingleChunkCapacity()
    {
        // arrange
        // 1024 + 2048 + 100 bytes fills chunks 0 and 1 completely and ends in chunk 2.
        var head = Head(0, 0);

        // act
        var (next, location, lastChunk) = ResultDocument.ComputeDataClaim(head, 3172);

        // assert
        Assert.Equal(0, location);
        Assert.Equal(2, lastChunk);
        Assert.Equal(Head(2, 100), next);
    }
}
