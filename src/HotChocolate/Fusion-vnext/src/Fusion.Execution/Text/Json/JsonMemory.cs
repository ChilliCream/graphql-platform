using System.Buffers;
using System.Runtime.InteropServices;
using HotChocolate.Fusion.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Manages the memory for storing JSON data.
/// </summary>
public static class JsonMemory
{
    public const int BufferSize = 1 << 17;

    private static readonly FixedSizeArrayPool s_pool = new(1, BufferSize, 128, preAllocate: true);
    private static readonly ArrayPool<byte[]> s_chunkPool = ArrayPool<byte[]>.Shared;

    public static byte[] Rent()
        => s_pool.Rent();

    public static byte[][] RentRange(int requiredChunks)
    {
        var chunks = s_chunkPool.Rent(requiredChunks);

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

    public static void Return(byte[][] chunks, int usedChunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (var chunk in chunks.AsSpan(0, usedChunks))
        {
            s_pool.Return(chunk);
        }
    }
}
