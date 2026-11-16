using System.Runtime.CompilerServices;
using HotChocolate.Buffers;

namespace HotChocolate.Utilities;

public class FixedSizeArrayPoolTests
{
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
