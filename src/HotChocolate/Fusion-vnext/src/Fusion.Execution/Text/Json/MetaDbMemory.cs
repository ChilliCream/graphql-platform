using HotChocolate.Fusion.Buffers;

namespace HotChocolate.Fusion.Text.Json;

public static class MetaDbMemory
{
    public const int ChunkSize = RowsPerChunk * CompositeResultDocument.DbRow.Size;
    public const int RowsPerChunk = 6552 * 1;

    private static readonly FixedSizeArrayPool s_pool = new(2, ChunkSize, 128, preAllocate: true);

    public static byte[] Rent()
        => s_pool.Rent();

    public static void Return(byte[] chunk)
        => s_pool.Return(chunk);
}
