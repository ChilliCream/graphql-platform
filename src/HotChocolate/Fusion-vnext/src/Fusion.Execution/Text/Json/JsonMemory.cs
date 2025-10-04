using System.Runtime.InteropServices;
using HotChocolate.Fusion.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Manages the memory for storing JSON data.
/// </summary>
internal static class JsonMemory
{
    /// <summary>
    /// The size of one JSON chunk.
    /// </summary>
    public const int ChunkSize = 128 * 1024;

    private static readonly FixedSizeArrayPool s_pool = new(1, ChunkSize, 64, preAllocate: false);

    public static byte[] Rent()
        => s_pool.Rent();

    public static byte[][] RentRange(int requiredChunks)
    {
        var chunks = new byte[requiredChunks][];

        for (var i = 0; i < requiredChunks; i++)
        {
            chunks[i] = s_pool.Rent();
        }

        return chunks;
    }

    public static void Return(byte[] chunk)
        => s_pool.Return(chunk);

    public static void Return(List<byte[]> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (var chunk in CollectionsMarshal.AsSpan(chunks))
        {
            s_pool.Return(chunk);
        }
    }

    public static void Return(byte[][] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (var chunk in chunks.AsSpan())
        {
            s_pool.Return(chunk);
        }
    }
}
