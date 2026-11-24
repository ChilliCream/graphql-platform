using System.Runtime.CompilerServices;

namespace HotChocolate.Buffers;

public class FixedSizeArrayPoolTests
{
    private const int ArraySize = 16;

    [Fact]
    public void Constructor_Should_ReserveSlotsOfStableLevelOnly_When_LaterLevelsAreLarger()
    {
        // arrange & act
        using var pool = CreatePool(4, 8, 425_633);

        // assert
        Assert.Equal(4, pool.Capacity);
    }

    [Fact]
    public void Rent_Should_ReserveSlotsOfNextLevel_When_CurrentLevelIsExhausted()
    {
        // arrange
        using var pool = CreatePool(4, 8, 425_633);

        // act
        RentMany(pool, 5);

        // assert
        Assert.Equal(8, pool.Capacity);
    }

    [Fact]
    public void Rent_Should_ReserveSlotsPageByPage_When_LevelSpansMoreThanOnePage()
    {
        // arrange
        using var pool = CreatePool(4, 5000);

        // act
        RentMany(pool, 5);
        var capacityInFirstPage = pool.Capacity;
        RentMany(pool, 4092);
        var capacityInSecondPage = pool.Capacity;

        // assert
        Assert.Equal(4096, capacityInFirstPage);
        Assert.Equal(5000, capacityInSecondPage);
    }

    [Fact]
    public void Rent_Should_ReturnPooledArray_When_ArrayWasReturned()
    {
        // arrange
        using var pool = CreatePool(4, 8);
        var first = pool.Rent();
        pool.Return(first);

        // act
        var second = pool.Rent();

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Rent_Should_ReuseEveryArray_When_PoolGrewIntoNextLevel()
    {
        // arrange
        using var pool = CreatePool(4, 8);
        var rented = RentMany(pool, 6);
        ReturnMany(pool, rented);

        // act
        var rentedAgain = RentMany(pool, 6);

        // assert
        Assert.Equal(6, rented.Intersect(rentedAgain, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Rent_Should_ReuseEveryArray_When_PoolGrewIntoSecondPage()
    {
        // arrange
        using var pool = CreatePool(4, 5000);
        var rented = RentMany(pool, 4200);
        ReturnMany(pool, rented);

        // act
        var rentedAgain = RentMany(pool, 4200);

        // assert
        Assert.Equal(4200, rented.Intersect(rentedAgain, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Rent_Should_AllocateArray_When_LastLevelIsExhausted()
    {
        // arrange
        using var pool = CreatePool(2, 4);
        var pooled = RentMany(pool, 4);

        // act
        var extra = pool.Rent();

        // assert
        Assert.Equal(ArraySize, extra.Length);
        Assert.Equal(5, pooled.Append(extra).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(4, pool.Capacity);
    }

    [Fact]
    public void Trim_Should_DropArraysAboveLowerLevel_When_FewArraysAreInUse()
    {
        // arrange
        using var pool = CreatePool(2, 4);
        var rented = RentMany(pool, 4);
        ReturnMany(pool, rented);

        // act
        pool.Trim();

        // assert
        var rentedAgain = RentMany(pool, 4);
        Assert.Equal(2, rented.Intersect(rentedAgain, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Trim_Should_KeepArrays_When_MoreArraysAreInUseThanLowerLevelHolds()
    {
        // arrange
        using var pool = CreatePool(2, 4);
        var rented = RentMany(pool, 3);

        // act
        pool.Trim();

        // assert
        ReturnMany(pool, rented);
        var rentedAgain = RentMany(pool, 3);
        Assert.Equal(3, rented.Intersect(rentedAgain, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Trim_Should_ReleaseSlots_When_PageLiesAboveLowerLevel()
    {
        // arrange
        using var pool = CreatePool(4, 10_000);
        var rented = RentMany(pool, 8200);
        var capacityBeforeTrim = pool.Capacity;
        ReturnMany(pool, rented);

        // act
        pool.Trim();

        // assert
        Assert.Equal(10_000, capacityBeforeTrim);
        Assert.Equal(4096, pool.Capacity);
    }

    [Fact]
    public void Trim_Should_KeepArraysBelowLowerLevel_When_LowerLevelEndsInsideAPage()
    {
        // arrange
        using var pool = CreatePool(4, 5000, 10_000);
        var rented = RentMany(pool, 10_000);
        ReturnMany(pool, rented);

        // act
        pool.Trim();
        var capacityAfterTrim = pool.Capacity;
        var rentedAgain = RentMany(pool, 5000);

        // assert
        // returned arrays fill the slots from the top down, so the last 5,000 returned stay pooled.
        Assert.Equal(8192, capacityAfterTrim);
        Assert.Equal(
            5000,
            rented.Skip(5000).Intersect(rentedAgain, ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Rent_Should_ReserveSlotsAgain_When_PoolGrowsAfterTrim()
    {
        // arrange
        using var pool = CreatePool(4, 10_000);
        ReturnMany(pool, RentMany(pool, 8200));
        pool.Trim();

        // act
        var rented = RentMany(pool, 8200);

        // assert
        Assert.Equal(10_000, pool.Capacity);
        Assert.Equal(8200, rented.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public void Constructor_Should_ReserveSlotsOfStableLevelOnly_When_PreAllocateIsSet()
    {
        // arrange & act
        using var pool = new FixedSizeArrayPool(
            poolId: 1,
            ArraySize,
            [3, 6, 425_633],
            TimeSpan.FromMinutes(10),
            preAllocate: true);

        // assert
        var rented = RentMany(pool, 3);
        Assert.Equal(3, pool.Capacity);
        Assert.All(rented, array => Assert.Equal(ArraySize, array.Length));
    }

    [Fact]
    public void Rent_Should_NotAllocateArrays_When_StableLevelWasPreAllocated()
    {
        // arrange
        const int arraySize = 4096;
        using var pool = new FixedSizeArrayPool(
            poolId: 1,
            arraySize,
            [3, 6],
            TimeSpan.FromMinutes(10),
            preAllocate: true);
        var rented = new byte[3][];
        pool.Return(pool.Rent());
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

        // act
        for (var i = 0; i < rented.Length; i++)
        {
            rented[i] = pool.Rent();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        // assert
        // without pre-allocation at least two of the three arrays would be allocated here.
        Assert.InRange(allocated, 0, arraySize - 1);
        Assert.Equal(3, rented.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Fact]
    public async Task Constructor_Should_NotRetainAsyncLocalValue_When_PoolIsCreatedInScope()
    {
        // arrange
        var local = new AsyncLocal<object?>();
        var (pool, reference) = CreatePoolInScope(local);

        // act
        var collected = await WaitForCollectionAsync(reference);

        // assert
        GC.KeepAlive(pool);
        pool.Dispose();
        Assert.True(collected);
    }

    private static FixedSizeArrayPool CreatePool(params int[] levels)
        => new(poolId: 1, ArraySize, levels, TimeSpan.FromMinutes(10), preAllocate: false);

    private static byte[][] RentMany(FixedSizeArrayPool pool, int count)
    {
        var arrays = new byte[count][];

        for (var i = 0; i < count; i++)
        {
            arrays[i] = pool.Rent();
        }

        return arrays;
    }

    private static void ReturnMany(FixedSizeArrayPool pool, byte[][] arrays)
    {
        foreach (var array in arrays)
        {
            pool.Return(array);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (FixedSizeArrayPool Pool, WeakReference Reference) CreatePoolInScope(
        AsyncLocal<object?> local)
    {
        var value = new object();
        local.Value = value;

        var pool = new FixedSizeArrayPool(
            poolId: 1,
            arraySize: 16,
            levels: [1, 2],
            trimInterval: TimeSpan.FromMinutes(1),
            preAllocate: false);

        local.Value = null;
        return (pool, new WeakReference(value));
    }

    private static async Task<bool> WaitForCollectionAsync(WeakReference reference)
    {
        for (var i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (!reference.IsAlive)
            {
                return true;
            }

            await Task.Delay(50);
        }

        return false;
    }
}
