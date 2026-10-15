namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentLocalPoolTests
{
    [Fact]
    public void Rent_Should_RentBatchFromSharedPool_When_NoArrayIsFree()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);

        // act
        var array = pool.Rent();

        // assert
        Snapshot.Create()
            .Add(
                new
                {
                    ArrayLength = array.Length,
                    SegmentArraySize = PathSegmentMemory.SegmentArraySize,
                    pool.OwnedCount,
                    pool.FreeCount
                },
                "Pool State")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Rent_Should_HandOutDistinctArrays_When_RentedRepeatedly()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var arrays = new HashSet<int[]>(ReferenceEqualityComparer.Instance);

        // act
        for (var i = 0; i < 100; i++)
        {
            arrays.Add(pool.Rent());
        }

        // assert
        Assert.Equal(100, arrays.Count);
        Assert.Equal(128, pool.OwnedCount);
    }

    [Fact]
    public void Rent_Should_ReuseReturnedArray_When_ArrayWasReturned()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var returned = pool.Rent();
        pool.Return(returned);

        // act
        var rented = pool.Rent();

        // assert
        Assert.Same(returned, rented);
    }

    [Fact]
    public void Return_Should_IgnoreArray_When_LengthDiffersFromSegmentSize()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var freeCount = pool.FreeCount;

        // act
        pool.Return(new int[PathSegmentMemory.SegmentArraySize + 1]);

        // assert
        Assert.Equal(freeCount, pool.FreeCount);
    }

    [Fact]
    public void Reset_Should_FreeEveryOwnedArray_When_OwnedCountIsWithinCapacity()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var first = pool.Rent();
        pool.Rent();

        // act
        pool.Reset();
        var ownedAfterReset = pool.OwnedCount;
        var rented = new HashSet<int[]>(ReferenceEqualityComparer.Instance);

        for (var i = 0; i < 32; i++)
        {
            rented.Add(pool.Rent());
        }

        // assert
        Snapshot.Create()
            .Add(
                new
                {
                    OwnedAfterReset = ownedAfterReset,
                    OwnedAfterRenting = pool.OwnedCount,
                    FirstArrayRentedAgain = rented.Contains(first)
                },
                "Pool State")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Reset_Should_ReturnExcessArraysToSharedPool_When_OwnedCountExceedsCapacity()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);

        for (var i = 0; i < 100; i++)
        {
            pool.Rent();
        }

        // act
        pool.Reset();

        // assert
        Assert.Equal(64, pool.OwnedCount);
        Assert.Equal(64, pool.FreeCount);
    }

    [Fact]
    public void Reset_Should_RetainConfiguredCapacity_When_CapacityIsBelowBatchSize()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 1);
        pool.Rent();
        pool.Rent();

        // act
        pool.Reset();

        // assert
        Assert.Equal(1, pool.OwnedCount);
        Assert.Equal(1, pool.FreeCount);
    }

    [Fact]
    public void Reset_Should_ShrinkBookkeeping_When_BurstExceededRetainedLength()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var rented = new List<int[]>();

        for (var i = 0; i < 10_000; i++)
        {
            rented.Add(pool.Rent());
        }

        foreach (var array in rented)
        {
            pool.Return(array);
        }

        var ownedCapacityAfterBurst = pool.OwnedCapacity;
        var freeCapacityAfterBurst = pool.FreeCapacity;

        // act
        pool.Reset();

        // assert
        Snapshot.Create()
            .Add(
                new
                {
                    OwnedCapacityAfterBurst = ownedCapacityAfterBurst,
                    FreeCapacityAfterBurst = freeCapacityAfterBurst,
                    pool.OwnedCount,
                    pool.FreeCount,
                    pool.OwnedCapacity,
                    pool.FreeCapacity
                },
                "Bookkeeping")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Reset_Should_DropOwnedArrays_When_SharedPoolWasReplaced()
    {
        // arrange
        using var replaced = new PathSegmentPool(
            segmentArraySize: 128,
            levels: [64, 128],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);
        using var pool = new PathSegmentLocalPool(capacity: 64, shared: replaced);
        var replacedArray = pool.Rent();

        // act
        pool.Reset();
        var currentArray = pool.Rent();

        // assert
        Snapshot.Create()
            .Add(
                new
                {
                    ReplacedArrayLength = replacedArray.Length,
                    CurrentArrayLength = currentArray.Length,
                    SegmentArraySize = PathSegmentMemory.SegmentArraySize,
                    pool.OwnedCount
                },
                "Pool State")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Dispose_Should_ReleaseEveryArray_When_Disposed()
    {
        // arrange
        var pool = new PathSegmentLocalPool(capacity: 64);
        pool.Rent();

        // act
        pool.Dispose();

        // assert
        Assert.Equal(0, pool.OwnedCount);
        Assert.Equal(0, pool.FreeCount);
    }

    [Fact]
    public void Reset_Should_AdvanceGeneration_When_Called()
    {
        // arrange
        using var pool = new PathSegmentLocalPool(capacity: 64);
        var generation = pool.Generation;

        // act
        pool.Reset();

        // assert
        Assert.Equal(generation + 1, pool.Generation);
    }
}
