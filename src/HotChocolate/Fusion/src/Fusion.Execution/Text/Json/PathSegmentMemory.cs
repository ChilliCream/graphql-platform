namespace HotChocolate.Fusion.Text.Json;

internal static class PathSegmentMemory
{
    private static PathSegmentPool s_pool = new(
        segmentArraySize: 64,
        levels: [
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
            13620260],
        trimInterval: TimeSpan.FromMinutes(1),
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
