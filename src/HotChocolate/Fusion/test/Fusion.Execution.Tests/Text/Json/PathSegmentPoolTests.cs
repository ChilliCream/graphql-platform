namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentPoolTests
{
    [Fact]
    public void Rent_Should_ReturnCachedArray_When_ArrayWasReturned()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var first = pool.Rent();

        // act
        pool.Return(first);
        var second = pool.Rent();

        // assert
        Assert.Same(first, second);
        Assert.Equal(1, pool.InUse);
    }

    [Fact]
    public void Return_Should_DropArray_When_MoreReturnedThanRented()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var rented = pool.Rent();
        pool.Return(rented);
        var foreign = new int[PathSegmentMemory.SegmentArraySize];

        // act
        // the pool has no outstanding slot, so the extra return is dropped rather than cached
        pool.Return(foreign);
        var reRented = pool.Rent();

        // assert
        Assert.Same(rented, reRented);
        Assert.NotSame(foreign, reRented);
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Return_Should_ReturnAllArrays_When_BatchSpanProvided()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var first = pool.Rent();
        var second = pool.Rent();
        var third = pool.Rent();
        int[]?[] batch = [first, second, third];

        // act
        pool.Return(batch);
        var inUseAfterReturn = pool.InUse;

        // assert
        // arrays come back in LIFO order, so the last returned is rented first
        Assert.Equal(0, inUseAfterReturn);
        Assert.Same(third, pool.Rent());
        Assert.Same(second, pool.Rent());
        Assert.Same(first, pool.Rent());
    }

    [Fact]
    public void Return_Should_SkipInvalidAndDropExcess_When_BatchHasNullsWrongLengthAndOverflow()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var first = pool.Rent();
        var second = pool.Rent();
        var third = pool.Rent();
        var fourth = pool.Rent();
        var overflow = pool.Rent(); // the fifth rent exhausts the pool and is not backed by a slot
        int[]?[] batch = [first, second, third, fourth, overflow, null, new int[8]];

        // act
        pool.Return(batch);

        // assert
        // the null and wrong-length entries are not counted, so in-use returns to zero; the overflow
        // array has no slot to land in and is dropped instead of throwing
        Assert.Equal(0, pool.InUse);
        Assert.Same(fourth, pool.Rent());
        Assert.NotSame(overflow, pool.Rent());
    }

    [Fact]
    public void Trim_Should_ReleaseUpperLevel_When_InUseBelowPreviousLimit()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var first = pool.Rent();
        var second = pool.Rent();
        var third = pool.Rent(); // climbs into the upper level
        pool.Return(first); // returned first, lands in the top (upper-level) slot
        pool.Return(second);
        pool.Return(third); // returned last, lands in slot 0

        // act
        pool.Trim();

        // assert
        // the upper-level slot is released, so its array is not handed back on re-rent
        Assert.Same(third, pool.Rent());
        Assert.Same(second, pool.Rent());
        Assert.NotSame(first, pool.Rent());
    }

    [Fact]
    public void Rent_Should_NotAllocate_When_PoolIsWarm()
    {
        Assert.SkipWhen(
            PathSegmentPoolEventSource.Log.IsEnabled(),
            "A tracing session has enabled the path segment pool EventSource; event emission allocates and invalidates the allocation measurement.");

        // arrange
        using var pool = new PathSegmentPool(
            PathSegmentMemory.SegmentArraySize,
            [32, 64],
            trimDueTime: Timeout.InfiniteTimeSpan,
            trimInterval: TimeSpan.FromSeconds(30),
            preAllocate: false);
        var holder = new int[]?[16];

        // warm the pool, the ArrayPool buckets, and tiered JIT before measuring
        for (var cycle = 0; cycle < 2; cycle++)
        {
            RunRentReturnCycle(pool, holder);
        }

        // act
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var cycle = 0; cycle < 100; cycle++)
        {
            RunRentReturnCycle(pool, holder);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // assert
        // a warm pool serves every rent from a cached slot and every batch return stores in place,
        // so a steady-state request lifecycle allocates nothing on the measuring thread
        Assert.Equal(0L, allocated);
    }

    private static void RunRentReturnCycle(PathSegmentPool pool, int[]?[] holder)
    {
        for (var i = 0; i < holder.Length; i++)
        {
            holder[i] = pool.Rent();
        }

        pool.Return(holder);
    }

    private static PathSegmentPool CreatePool(int[] levels)
        => new(
            PathSegmentMemory.SegmentArraySize,
            levels,
            trimDueTime: Timeout.InfiniteTimeSpan,
            trimInterval: TimeSpan.FromSeconds(30),
            preAllocate: false);
}
