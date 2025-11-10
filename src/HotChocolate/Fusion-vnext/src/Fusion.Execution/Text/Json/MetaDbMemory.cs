using HotChocolate.Fusion.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public static class MetaDbMemory
{
    public const int BufferSize = 1 << 17;
    public const int RowsPerChunk = 6552;

    private static readonly FixedSizeArrayPool s_pool = new(2, BufferSize, 128 * 6, preAllocate: true);

    public static byte[] Rent()
        => s_pool.Rent();

    public static void Return(byte[] chunk)
        => s_pool.Return(chunk);
}
