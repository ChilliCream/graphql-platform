using HotChocolate.Fusion.Buffers;

namespace HotChocolate.Fusion.Text.Json;

internal static class MetaDbMemory
{
    public const int ChunkSize = RowsPerChunk * CompositeResultDocument.DbRow.Size;
    public const int RowsPerChunk = 6552;

    private static readonly FixedSizeArrayPool s_pool = new(1, ChunkSize, 64, preAllocate: false);

    public static byte[] Rent()
        => s_pool.Rent();

    public static void Return(byte[] chunk)
        => s_pool.Return(chunk);
}
