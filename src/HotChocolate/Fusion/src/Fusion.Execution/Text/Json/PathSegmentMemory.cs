namespace HotChocolate.Fusion.Text.Json;

internal static class PathSegmentMemory
{
    private static PathSegmentPool s_pool = new(
        segmentArraySize: 64,
        levels: [4096, 8192, 16384, 32768, 65536],
        trimInterval: TimeSpan.FromMinutes(5),
        preAllocate: false);

    public static int SegmentArraySize => s_pool._segmentArraySize;

    public static void Reconfigure(Func<PathSegmentPool> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        var oldPool = Interlocked.Exchange(
            ref s_pool,
            factory() ?? throw new InvalidOperationException(
                "The factory must create a valid pool."));
        oldPool.Dispose();
    }

    public static int[] Rent() => s_pool.Rent();

    public static void Return(int[] array) => s_pool.Return(array);
}
