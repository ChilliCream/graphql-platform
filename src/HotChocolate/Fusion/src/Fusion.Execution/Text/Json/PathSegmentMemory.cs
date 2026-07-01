namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Provides pooled <c>int[]</c> segment buffers that back <see cref="CompactPath"/> instances.
/// The pool is striped per CPU core so that concurrent path building on many threads does not
/// contend on a single lock.
/// </summary>
internal static class PathSegmentMemory
{
    // Cache-capacity thresholds for a single (non-striped) pool. When the pool is striped across
    // cores these are divided by the stripe count so the total cache size stays roughly constant.
    private static readonly int[] s_baseLevels =
    [
        4096,
        6144,
        9216,
        13824,
        20736,
        31104,
        46656,
        69984,
        104976,
        157464,
        236196,
        354294,
        531441,
        797162,
        1195743,
        1793614,
        2690422,
        4035632,
        6053449,
        9080173,
        13620260
    ];

    private static PathSegmentPool[] s_pools = CreateDefaultPools();

    private static PathSegmentPool[] CreateDefaultPools()
    {
        var count = StripeCount;
        var levels = new int[s_baseLevels.Length];
        for (var i = 0; i < s_baseLevels.Length; i++)
        {
            // Keep the total cache capacity across all stripes close to the single-pool sizing.
            levels[i] = Math.Max(32, s_baseLevels[i] / count);
        }

        var pools = new PathSegmentPool[count];
        for (var i = 0; i < count; i++)
        {
            pools[i] = new PathSegmentPool(
                segmentArraySize: 64,
                levels: levels,
                trimInterval: TimeSpan.FromMinutes(1),
                preAllocate: false);
        }

        return pools;
    }

    private static int StripeCount => Math.Max(1, Environment.ProcessorCount);

    private static PathSegmentPool Current
    {
        get
        {
            var pools = s_pools;

            if (pools.Length == 1)
            {
                return pools[0];
            }

            var index = (int)((uint)Thread.GetCurrentProcessorId() % (uint)pools.Length);
            return pools[index];
        }
    }

    public static int SegmentArraySize => s_pools[0]._segmentArraySize;

    public static void Reconfigure(Func<PathSegmentPool> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var count = StripeCount;
        var newPools = new PathSegmentPool[count];
        for (var i = 0; i < count; i++)
        {
            newPools[i] = factory() ?? throw new InvalidOperationException(
                "The factory must create a valid pool.");
        }

        var oldPools = Interlocked.Exchange(ref s_pools, newPools);
        foreach (var oldPool in oldPools)
        {
            oldPool.Dispose();
        }
    }

    public static int[] Rent() => Current.Rent();

    public static void Return(int[] array) => Current.Return(array);
}
