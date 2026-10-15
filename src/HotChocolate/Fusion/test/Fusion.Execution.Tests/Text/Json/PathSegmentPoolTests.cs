namespace HotChocolate.Fusion.Text.Json;

public class PathSegmentPoolTests
{
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
}
