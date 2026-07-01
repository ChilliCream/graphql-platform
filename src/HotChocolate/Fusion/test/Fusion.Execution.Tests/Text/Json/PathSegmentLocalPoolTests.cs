namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentLocalPoolTests
{
    [Fact]
    public void Dispose_Should_ReturnAllRentedToPinnedPool_When_DisposedOnDifferentThread()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var localPool = new PathSegmentLocalPool(pool, initialCapacity: 32);
        localPool.Rent();
        localPool.Rent();
        localPool.Rent();

        // act
        // dispose on a dedicated thread, not the rent thread, to exercise the cross-thread teardown path
        var disposeThread = new Thread(localPool.Dispose);
        disposeThread.Start();
        disposeThread.Join();

        // assert
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Dispose_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        using var pool = CreatePool([2, 4]);
        var localPool = new PathSegmentLocalPool(pool, initialCapacity: 32);
        localPool.Rent();
        localPool.Rent();

        // act
        localPool.Dispose();
        localPool.Dispose();

        // assert
        // the second dispose is a no-op and does not double-return arrays to the pool
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Dispose_Should_OnlyAllocateLocalPoolInstance_When_LifecyclesAreWarm()
    {
        Assert.SkipWhen(
            PathSegmentPoolEventSource.Log.IsEnabled(),
            "A tracing session has enabled the path segment pool EventSource; event emission allocates and invalidates the allocation measurement.");

        // arrange
        const int cycles = 100;
        using var pool = new PathSegmentPool(
            PathSegmentMemory.SegmentArraySize,
            [32, 64],
            trimDueTime: Timeout.InfiniteTimeSpan,
            trimInterval: TimeSpan.FromSeconds(30),
            preAllocate: false);
        var holder = new int[]?[8];

        // warm the pool, the ArrayPool buckets, and tiered JIT before measuring
        for (var warmup = 0; warmup < 2; warmup++)
        {
            RunLifecycle(pool, holder);
        }

        // act
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var cycle = 0; cycle < cycles; cycle++)
        {
            RunLifecycle(pool, holder);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // assert
        // the only unavoidable per-cycle allocation is the PathSegmentLocalPool instance itself;
        // every buffer comes from the warm pools, so the total stays within a tight per-cycle bound
        Assert.InRange(allocated, 0, cycles * 128);
    }

    private static void RunLifecycle(PathSegmentPool pool, int[]?[] holder)
    {
        var localPool = new PathSegmentLocalPool(pool, initialCapacity: 32);

        for (var i = 0; i < 16; i++)
        {
            var array = localPool.Rent();

            if (i < holder.Length)
            {
                holder[i] = array;
            }
        }

        for (var i = 0; i < holder.Length; i++)
        {
            localPool.Return(holder[i]!);
        }

        for (var i = 0; i < holder.Length; i++)
        {
            localPool.Rent();
        }

        localPool.Dispose();
    }

    private static PathSegmentPool CreatePool(int[] levels)
        => new(
            PathSegmentMemory.SegmentArraySize,
            levels,
            trimDueTime: Timeout.InfiniteTimeSpan,
            trimInterval: TimeSpan.FromSeconds(30),
            preAllocate: false);
}
