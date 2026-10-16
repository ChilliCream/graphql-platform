using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentPoolTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Create_Should_ReserveOnlyInitialLevel_When_MaximumLevelIsLarge(bool preAllocate)
    {
        // arrange
        // act
        using var pool = new PathSegmentPool(64, [4, 13620260], TimeSpan.FromMinutes(1), preAllocate);

        // assert
        Assert.Equal(4, pool.Capacity);
        Assert.Equal(0, pool.InUse);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    [InlineData(4, 4)]
    [InlineData(5, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    [InlineData(16, 16)]
    [InlineData(17, 16)]
    public void Rent_Should_GrowToRequiredLevel_When_CrossingCapacity(int count, int capacity)
    {
        // arrange
        using var pool = new PathSegmentPool(64, [4, 8, 16], TimeSpan.FromMinutes(1), false);
        var arrays = new int[]?[count];

        // act
        pool.Rent(arrays);

        // assert
        Assert.Equal(capacity, pool.Capacity);
        Assert.Equal(count, pool.InUse);
        Assert.All(arrays, array => Assert.Equal(64, array?.Length));
        Assert.Equal(count, new HashSet<int[]?>(arrays, ReferenceEqualityComparer.Instance).Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rent_Should_PreserveReturnedArrays_When_GrowingWithCachedBuffers(bool preAllocate)
    {
        // arrange
        using var pool = new PathSegmentPool(64, [4, 8, 16], TimeSpan.FromMinutes(1), preAllocate);
        var first = new int[]?[4];
        pool.Rent(first);
        pool.Return(first);
        var next = new int[]?[12];

        // act
        pool.Rent(next);

        // assert
        Assert.True(new HashSet<int[]?>(first, ReferenceEqualityComparer.Instance).IsSubsetOf(next));
        Assert.Equal(12, new HashSet<int[]?>(next, ReferenceEqualityComparer.Instance).Count);
        Assert.All(next, array => Assert.Equal(64, array?.Length));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Trim_Should_KeepFreeArrays_When_PoolShrinks(bool preAllocate)
    {
        // arrange
        using var pool = new PathSegmentPool(64, [4, 8, 16], TimeSpan.FromMinutes(1), preAllocate);
        var arrays = new int[]?[16];
        pool.Rent(arrays);
        pool.Return(arrays);

        // act
        pool.Trim();
        var next = new int[]?[8];
        pool.Rent(next);

        // assert
        Assert.Equal(8, pool.Capacity);
        Assert.Equal(8, pool.InUse);
        Assert.Equal(
            new HashSet<int[]?>(arrays.Skip(8), ReferenceEqualityComparer.Instance),
            new HashSet<int[]?>(next, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Trim_Should_RespectOutstandingRentals_When_PoolWasExhausted()
    {
        // arrange
        using var pool = new PathSegmentPool(64, [4, 8, 16], TimeSpan.FromMinutes(1), false);
        var arrays = new int[]?[20];
        pool.Rent(arrays);

        // act
        pool.Trim();
        var occupiedCapacity = pool.Capacity;
        pool.Return(arrays.AsSpan(0, 12));
        pool.Trim();
        var partialCapacity = pool.Capacity;
        pool.Return(arrays.AsSpan(12));
        pool.Trim();
        pool.Trim();

        // assert
        Assert.Equal(16, occupiedCapacity);
        Assert.Equal(8, partialCapacity);
        Assert.Equal(4, pool.Capacity);
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Rent_Should_PreserveExclusiveOwnership_When_TrimmingConcurrentBatches()
    {
        // arrange
        using var pool = new PathSegmentPool(64, [4, 16, 64], TimeSpan.FromMinutes(1), false);
        var active = new ConcurrentDictionary<int[], byte>(ReferenceEqualityComparer.Instance);
        var failures = new ConcurrentQueue<string>();

        // act
        Parallel.For(0, 8, worker =>
        {
            var arrays = new int[]?[53];

            for (var iteration = 0; iteration < 1000; iteration++)
            {
                pool.Rent(arrays);

                foreach (var array in arrays)
                {
                    if (array is not { Length: 64 } || !active.TryAdd(array, 0))
                    {
                        failures.Enqueue("Invalid or concurrently rented array.");
                    }
                }

                foreach (var array in arrays)
                {
                    if (array is not null)
                    {
                        active.TryRemove(array, out _);
                    }
                }

                pool.Return(arrays);
                pool.Trim();
            }
        });

        // assert
        Assert.Empty(failures);
        Assert.Empty(active);
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Rent_Should_FillEverySlot_When_PoolIsExhausted()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var arrays = new int[]?[12];

        // act
        pool.Rent(arrays);

        // assert
        Assert.All(arrays, array => Assert.Equal(64, array?.Length));
    }

    [Fact]
    public void Rent_Should_HandOutReturnedArrays_When_ArraysWereReturned()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var first = new int[]?[3];
        pool.Rent(first);
        pool.Return(first);
        var second = new int[]?[3];

        // act
        pool.Rent(second);

        // assert
        Assert.Equal(
            new HashSet<int[]?>(first, ReferenceEqualityComparer.Instance),
            new HashSet<int[]?>(second, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Return_Should_DropArray_When_LengthDiffersFromSegmentSize()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var first = new int[]?[2];
        pool.Rent(first);
        var foreign = new int[128];

        // act
        pool.Return([first[0], foreign, first[1]]);
        var second = new int[]?[3];
        pool.Rent(second);

        // assert
        Assert.All(second, array => Assert.Equal(64, array?.Length));
        Assert.Equal(2, second.Intersect(first, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Return_Should_KeepInUseCount_When_ArrayIsRejected()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var rented = new int[]?[2];
        pool.Rent(rented);

        // act
        pool.Return([rented[0], new int[128], null, rented[1]]);

        // assert
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Return_Should_DecrementInUseCount_When_PoolHasNoRoom()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var rented = new int[]?[12];
        pool.Rent(rented);
        var inUseAfterRent = pool.InUse;

        // act
        pool.Return(rented);

        // assert
        Assert.Equal(12, inUseAfterRent);
        Assert.Equal(0, pool.InUse);
    }

    [Fact]
    public void Rent_Should_HandOutEveryReturnedArray_When_BatchExceedsLockBatchSize()
    {
        // arrange
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 256],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var first = new int[]?[200];
        pool.Rent(first);
        pool.Return(first);
        var inUseAfterReturn = pool.InUse;
        var second = new int[]?[200];

        // act
        pool.Rent(second);

        // assert
        Assert.Equal(0, inUseAfterReturn);
        Assert.Equal(200, pool.InUse);
        Assert.Equal(
            new HashSet<int[]?>(first, ReferenceEqualityComparer.Instance),
            new HashSet<int[]?>(second, ReferenceEqualityComparer.Instance));
    }

    [Fact]
    public void Return_Should_IgnoreNullEntries_When_EventSourceIsEnabled()
    {
        // arrange
        using var listener = new VerboseListener();
        listener.EnableEvents(PathSegmentPoolEventSource.Log, EventLevel.Verbose);
        using var pool = new PathSegmentPool(
            segmentArraySize: 64,
            levels: [4, 8],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        var rented = new int[]?[2];
        pool.Rent(rented);

        // act
        pool.Return([rented[0], null, rented[1]]);

        // assert
        Assert.True(PathSegmentPoolEventSource.Log.IsEnabled());
        Assert.Equal(0, pool.InUse);
    }

    private sealed class VerboseListener : EventListener;
}
