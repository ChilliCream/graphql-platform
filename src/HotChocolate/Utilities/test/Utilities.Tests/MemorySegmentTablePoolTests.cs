namespace HotChocolate.Buffers;

[Collection(MemorySegmentTablePoolCollection.Name)]
public class MemorySegmentTablePoolTests
{
    [Fact]
    public void Rent_Should_RoundUpToBucketLength_When_BelowSmallestBucket()
    {
        // arrange & act
        // a metadata table asks for 9 entries; it must round up to the smallest bucket length of 16.
        var table = MemorySegmentTablePool.Rent(9);

        // assert
        Assert.Equal(16, table.Length);
    }

    [Fact]
    public void Rent_Should_RoundUpToNextPowerOfTwo_When_Odd()
    {
        // arrange & act
        var table = MemorySegmentTablePool.Rent(40);

        // assert
        Assert.Equal(64, table.Length);
    }

    [Fact]
    public void Rent_Should_ReturnRecycledTable_When_PreviouslyReturned()
    {
        // arrange
        // a bucket recycles in LIFO order, so the table just returned is the next one handed out.
        var table = MemorySegmentTablePool.Rent(16);
        MemorySegmentTablePool.Return(table);

        // act
        var recycled = MemorySegmentTablePool.Rent(16);

        // assert
        Assert.Same(table, recycled);
    }

    [Fact]
    public void Return_Should_ClearTable_When_Returned()
    {
        // arrange
        // a returned table must not keep a page alive, so its entries are cleared before parking.
        var table = MemorySegmentTablePool.Rent(16);
        table[0] = new MemorySegment(new byte[8], 0, 8);

        // act
        MemorySegmentTablePool.Return(table);
        var recycled = MemorySegmentTablePool.Rent(16);

        // assert
        Assert.Same(table, recycled);
        Assert.Null(recycled[0].Buffer);
    }

    [Fact]
    public void Rent_Should_ReturnClearedTable_When_ReusingFallbackTable()
    {
        // arrange
        // 256 is above the largest bucket length of 128, so the private fallback pool serves it.
        var table = MemorySegmentTablePool.Rent(256);
        Array.Fill(table, new MemorySegment(new byte[8], 1, 2));
        MemorySegmentTablePool.Return(table);

        // act
        var recycled = MemorySegmentTablePool.Rent(256);

        try
        {
            // assert
            Assert.True(recycled.Length >= 256);
            Assert.All(
                recycled,
                static segment =>
                {
                    Assert.Null(segment.Buffer);
                    Assert.Equal(0, segment.Offset);
                    Assert.Equal(0, segment.Length);
                });
        }
        finally
        {
            MemorySegmentTablePool.Return(recycled);
        }
    }
}
