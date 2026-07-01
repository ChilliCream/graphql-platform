namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Provides pooled <c>int[]</c> segment buffers that back <see cref="CompactPath"/> instances.
/// The pool is striped across several independent pools so that concurrent path building does
/// not contend on a single lock. Callers obtain a pool for the lifetime of a request through
/// <see cref="GetPool"/> and issue all rents and returns against that pool.
/// </summary>
internal static class PathSegmentMemory
{
    /// <summary>
    /// The length of every pooled segment buffer, including the leading length slot.
    /// </summary>
    public const int SegmentArraySize = 64;

    // Cache-capacity thresholds for a single (non-striped) pool. When the pool is striped these
    // are divided by the stripe count so the total cache size stays roughly constant.
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

    private static readonly PathSegmentPool[] s_pools = CreateDefaultPools();
    private static int s_nextPool = -1;

    // The number of stripes is capped so that per-stripe caches stay usefully sized and the
    // number of pools and trim timers stays bounded on machines with very high core counts.
    // At least two stripes are kept because request concurrency is not bounded by core count,
    // so striping stays effective on low-core hosts.
    private static int StripeCount => Math.Min(Math.Max(2, Environment.ProcessorCount), 64);

    private static PathSegmentPool[] CreateDefaultPools()
    {
        var count = StripeCount;
        var trimInterval = TimeSpan.FromMinutes(1);
        var levels = DivideLevels(s_baseLevels, count);

        var pools = new PathSegmentPool[count];
        for (var i = 0; i < count; i++)
        {
            pools[i] = new PathSegmentPool(
                segmentArraySize: SegmentArraySize,
                levels: levels,
                // Stagger the due times so the stripe trim timers do not all take their locks
                // in lockstep once per interval.
                trimDueTime: trimInterval + i * trimInterval / count,
                trimInterval: trimInterval,
                preAllocate: false);
        }

        return pools;
    }

    /// <summary>
    /// Selects a pool for a caller. Selection is round-robin so that concurrent requests spread
    /// evenly across the stripes regardless of which core they run on.
    /// </summary>
    public static PathSegmentPool GetPool()
        => s_pools[SelectPoolIndex(ref s_nextPool, s_pools.Length)];

    /// <summary>
    /// Computes the next round-robin pool index for the given cursor and pool count. A single pool
    /// short-circuits to index zero without advancing the cursor; otherwise the cursor is advanced
    /// atomically and wrapped into range so the selection stays valid across cursor overflow.
    /// </summary>
    internal static int SelectPoolIndex(ref int cursor, int poolCount)
        => poolCount == 1 ? 0 : (int)((uint)Interlocked.Increment(ref cursor) % (uint)poolCount);

    /// <summary>
    /// Divides the base capacity ladder by <paramref name="count"/> so the total cache size across
    /// all stripes stays close to the single-pool sizing. Levels that do not grow after division are
    /// dropped so the returned ladder is strictly increasing for any stripe count; the top capacity
    /// is always preserved.
    /// </summary>
    internal static int[] DivideLevels(ReadOnlySpan<int> baseLevels, int count)
    {
        var levels = new int[baseLevels.Length];
        var length = 0;
        var previous = 0;

        for (var i = 0; i < baseLevels.Length; i++)
        {
            var level = Math.Max(32, baseLevels[i] / count);

            // Equal adjacent levels defeat the bucket's level ladder, so only keep a level once it
            // grows past the previous one. Plateaus at the top carry the same capacity forward.
            if (level > previous)
            {
                levels[length++] = level;
                previous = level;
            }
        }

        return levels.AsSpan(0, length).ToArray();
    }
}
