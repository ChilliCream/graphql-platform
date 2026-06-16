namespace HotChocolate.Buffers;

[Collection(MemorySegmentTablePoolCollection.Name)]
public class MemoryArenaTests
{
    [Fact]
    public void Rent_Should_CarveSlabOfRequestedSize_When_Called()
    {
        // arrange
        using var arena = new MemoryArena();

        // act
        var segment = arena.Rent(100);

        // assert
        Assert.Equal(0, segment.Offset);
        Assert.Equal(100, segment.Length);
        Assert.Equal(JsonMemory.BufferSize, segment.Buffer.Length);
        Assert.Equal(1, arena.RentedPageCount);
    }

    [Fact]
    public void Rent_Should_PackSlabsIntoSamePage_When_TheyFit()
    {
        // arrange
        using var arena = new MemoryArena();

        // act
        var first = arena.Rent(100);
        var second = arena.Rent(200);

        // assert
        Assert.Same(first.Buffer, second.Buffer);
        Assert.Equal(0, first.Offset);
        Assert.Equal(100, second.Offset);
        Assert.Equal(1, arena.RentedPageCount);
    }

    [Fact]
    public void Rent_Should_RollToNewPage_When_SlabDoesNotFit()
    {
        // arrange
        using var arena = new MemoryArena();

        // act
        var first = arena.Rent(JsonMemory.BufferSize);
        var second = arena.Rent(1);

        // assert
        Assert.NotSame(first.Buffer, second.Buffer);
        Assert.Equal(0, second.Offset);
        Assert.Equal(2, arena.RentedPageCount);
    }

    [Fact]
    public void Rent_Should_Throw_When_SizeExceedsPage()
    {
        // arrange
        using var arena = new MemoryArena();

        // act
        void Act() => arena.Rent(JsonMemory.BufferSize + 1);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }

    [Fact]
    public void Rent_Should_Throw_When_ArenaIsDisposed()
    {
        // arrange
        var arena = new MemoryArena();
        arena.Dispose();

        // act
        void Act() => arena.Rent(1);

        // assert
        Assert.Throws<ObjectDisposedException>(Act);
    }

    [Fact]
    public void Rent_Should_Throw_When_ArenaIsSealed()
    {
        // arrange
        using var arena = new MemoryArena();
        arena.Rent(1);
        arena.Seal();

        // act
        void Act() => arena.Rent(1);

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Dispose_Should_ReturnPagesToPool_When_Sealed()
    {
        // arrange
        // the fixed-size pool hands buffers back in LIFO order, so a sealed page returned on
        // dispose is the next page handed out, which proves it was recycled.
        var first = new MemoryArena();
        var page = first.Rent(1).Buffer;
        first.Seal();

        // act
        first.Dispose();
        using var second = new MemoryArena();
        var recycled = second.Rent(1).Buffer;

        // assert
        Assert.Same(page, recycled);
    }

    [Fact]
    public void Dispose_Should_AbandonPages_When_NotSealed()
    {
        // arrange
        // an unsealed dispose abandons its pages, so the page can never be handed out again.
        var first = new MemoryArena();
        var page = first.Rent(1).Buffer;

        // act
        first.Dispose();
        using var second = new MemoryArena();
        var rented = second.Rent(1).Buffer;

        // assert
        Assert.NotSame(page, rented);
    }

    [Fact]
    public void Dispose_Should_Succeed_When_SealedWithoutRentals()
    {
        // arrange
        var arena = new MemoryArena();
        arena.Seal();

        // act
        var ex = Record.Exception(arena.Dispose);

        // assert
        Assert.Null(ex);
        Assert.True(arena.IsDisposed);
        Assert.Equal(0, arena.RentedPageCount);
    }

    [Fact]
    public void Dispose_Should_BeIdempotent_When_Sealed()
    {
        // arrange
        var arena = new MemoryArena();
        arena.Rent(1);
        arena.Seal();

        // act
        arena.Dispose();
        var ex = Record.Exception(arena.Dispose);

        // assert
        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_Should_BeIdempotent_When_NotSealed()
    {
        // arrange
        var arena = new MemoryArena();
        arena.Rent(1);

        // act
        arena.Dispose();
        var ex = Record.Exception(arena.Dispose);

        // assert
        Assert.Null(ex);
    }

    [Fact]
    public void RentSegmentTable_Should_TrackTable_When_Rented()
    {
        // arrange
        using var arena = new MemoryArena();

        // act
        var table = arena.RentSegmentTable(16);

        // assert
        Assert.True(table.Length >= 16);
        Assert.Equal(1, arena.RentedTableCount);
    }

    [Fact]
    public void GrowSegmentTable_Should_CopyEntriesAndKeepSingleTracked_When_Grown()
    {
        // arrange
        // growing must not leave the old table tracked, otherwise dispose would return it twice.
        using var arena = new MemoryArena();
        var table = arena.RentSegmentTable(4);
        var originalLength = table.Length;
        table[0] = new MemorySegment(new byte[8], 1, 2);

        // act
        arena.GrowSegmentTable(ref table);

        // assert
        Assert.True(table.Length >= originalLength * 2);
        Assert.Equal(1, table[0].Offset);
        Assert.Equal(1, arena.RentedTableCount);
    }

    [Fact]
    public void Dispose_Should_ReturnTablesToPool_When_Sealed()
    {
        // arrange
        // the table pool hands tables back in LIFO order, so a sealed table returned on dispose is
        // the next table of that length handed out, which proves it was recycled.
        var first = new MemoryArena();
        var table = first.RentSegmentTable(16);
        first.Seal();

        // act
        first.Dispose();
        var recycled = MemorySegmentTablePool.Rent(16);

        // assert
        Assert.Same(table, recycled);
    }

    [Fact]
    public void Dispose_Should_AbandonTables_When_NotSealed()
    {
        // arrange
        // an unsealed dispose abandons its tables, so a live document may still index them safely.
        var first = new MemoryArena();
        var table = first.RentSegmentTable(16);

        // act
        first.Dispose();
        var rented = MemorySegmentTablePool.Rent(16);

        // assert
        Assert.NotSame(table, rented);
    }

    [Fact]
    public void RentSegmentTable_Should_TrackAllTables_When_ExceedingTrackerCapacity()
    {
        // arrange
        // renting past the pooled tracker capacity grows the tracker into a larger fallback array;
        // every table must stay tracked across the grow.
        using var arena = new MemoryArena();
        var count = MemorySegmentTableTrackerPool.Capacity + 4;

        // act
        for (var i = 0; i < count; i++)
        {
            arena.RentSegmentTable(16);
        }

        // assert
        Assert.Equal(count, arena.RentedTableCount);
    }

    [Fact]
    public void Dispose_Should_ReturnTablesToPool_When_SealedAfterTrackerGrew()
    {
        // arrange
        // after the tracker grows past its pooled capacity, a sealed dispose must still return every
        // tracked table exactly once; the last table returned is the next one handed out.
        var first = new MemoryArena();
        var count = MemorySegmentTableTrackerPool.Capacity + 1;
        MemorySegment[] last = null!;
        for (var i = 0; i < count; i++)
        {
            last = first.RentSegmentTable(16);
        }
        first.Seal();

        // act
        first.Dispose();
        var recycled = MemorySegmentTablePool.Rent(16);

        // assert
        Assert.Same(last, recycled);
    }

    [Fact]
    public void Dispose_Should_ClearTableTracking_When_Disposed()
    {
        // arrange
        var arena = new MemoryArena();
        arena.RentSegmentTable(16);
        arena.Seal();

        // act
        arena.Dispose();

        // assert
        Assert.Equal(0, arena.RentedTableCount);
    }

    [Fact]
    public void Rent_Should_ProduceNonOverlappingSegments_When_Concurrent()
    {
        // arrange
        using var arena = new MemoryArena();
        const int count = 4000;
        const int size = 64;
        var segments = new MemorySegment[count];

        // act
        Parallel.For(0, count, i => segments[i] = arena.Rent(size));

        // assert
        // within every page, ordering by offset must yield strictly non-overlapping slabs.
        var overlaps = segments
            .GroupBy(s => s.Buffer)
            .Any(page =>
            {
                var ordered = page.OrderBy(s => s.Offset).ToArray();
                for (var i = 1; i < ordered.Length; i++)
                {
                    if (ordered[i - 1].Offset + ordered[i - 1].Length > ordered[i].Offset)
                    {
                        return true;
                    }
                }

                return false;
            });

        Assert.False(overlaps);
    }
}
