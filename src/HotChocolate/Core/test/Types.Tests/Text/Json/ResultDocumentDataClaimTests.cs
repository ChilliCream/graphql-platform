namespace HotChocolate.Text.Json;

public class ResultDocumentDataClaimTests
{
    // The data store is a single linear byte space. Chunk byte sizes follow the geometric schedule:
    // chunk 0 holds 1024 bytes, chunk 1 holds 2048, doubling up to 131072 from chunk 7 onward. The
    // ramp (chunks 0..7) holds 1024 * 255 = 261120 bytes before the constant 128K tail begins.
    private const int LastChunk = 4095;
    private const int LargestChunkBytes = 131_072;
    private const long RampTotalBytes = 255L * 1024;

    // The linear byte position of the first byte of the given chunk.
    private static long Start(int chunk)
        => chunk < 8
            ? 1024L * ((1L << chunk) - 1)
            : RampTotalBytes + ((long)(chunk - 8) * LargestChunkBytes);

    private static int Location(int chunk, int offset) => (chunk << 17) | offset;

    [Fact]
    public void ComputeDataClaim_Should_Throw_When_DataCapacityExceeded()
    {
        // arrange
        // the head sits near the end of the last chunk, so the value spills into chunk 4096.
        var start = Start(LastChunk) + (LargestChunkBytes - 100);

        // act
        void Act() => ResultDocument.ComputeDataClaim(start, 200);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void ComputeDataClaim_Should_Succeed_When_ClaimFillsLastChunkExactly()
    {
        // arrange
        var start = Start(LastChunk);

        // act
        var (location, lastChunk) = ResultDocument.ComputeDataClaim(start, LargestChunkBytes);

        // assert
        Assert.Equal(LastChunk, lastChunk);
        Assert.Equal(Location(LastChunk, 0), location);
    }

    [Fact]
    public void ComputeDataClaim_Should_SpanIntoNextChunkWithoutGap_When_ValueDoesNotFitCurrentChunk()
    {
        // arrange
        // a 2000-byte value at offset 100 of chunk 0 (1024 bytes) starts exactly at the head
        // and spans into chunk 1: 924 bytes fill chunk 0 and 1076 land in chunk 1.
        var start = Start(0) + 100;

        // act
        var (location, lastChunk) = ResultDocument.ComputeDataClaim(start, 2000);

        // assert
        Assert.Equal(Location(0, 100), location);
        Assert.Equal(1, lastChunk);
    }

    [Fact]
    public void ComputeDataClaim_Should_SpanRampChunks_When_ValueExceedsSingleChunkCapacity()
    {
        // arrange
        // 1024 + 2048 + 100 bytes fills chunks 0 and 1 completely and ends in chunk 2.
        var start = Start(0);

        // act
        var (location, lastChunk) = ResultDocument.ComputeDataClaim(start, 3172);

        // assert
        Assert.Equal(Location(0, 0), location);
        Assert.Equal(2, lastChunk);
    }

    [Fact]
    public void ComputeDataClaim_Should_DecodeRampStart_When_HeadSitsInsideRampChunk()
    {
        // arrange
        // chunk 3 starts at byte 1024+2048+4096 = 7168; offset 50 lands at byte 7218.
        var start = Start(3) + 50;

        // act
        var (location, lastChunk) = ResultDocument.ComputeDataClaim(start, 10);

        // assert
        Assert.Equal(Location(3, 50), location);
        Assert.Equal(3, lastChunk);
    }

    [Fact]
    public void ComputeDataClaim_Should_DecodeTailStart_When_HeadSitsPastRamp()
    {
        // arrange
        // chunk 10 is the third tail chunk; offset 77 inside it.
        var start = Start(10) + 77;

        // act
        var (location, lastChunk) = ResultDocument.ComputeDataClaim(start, 5);

        // assert
        Assert.Equal(Location(10, 77), location);
        Assert.Equal(10, lastChunk);
    }
}
