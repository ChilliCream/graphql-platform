using System.Buffers;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using static HotChocolate.Buffers.JsonMemoryEventSource;

namespace HotChocolate.Buffers;

/// <summary>
/// Manages the memory for storing JSON data.
/// </summary>
internal static class JsonMemory
{
    public const int BufferSize = 1 << 17;

    private static FixedSizeArrayPool s_pool = new(FixedSizeArrayPoolKinds.JsonMemory, BufferSize, 128);
    private static readonly ArrayPool<byte[]> s_chunkPool = ArrayPool<byte[]>.Shared;

    public static void Reconfigure(Func<FixedSizeArrayPool> factory)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(factory);
#else
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
#endif

        s_pool = factory() ?? throw new InvalidOperationException("The factory must create a valid pool.");
        Log.ReconfiguredPool();
    }

    public static byte[] Rent(JsonMemoryKind kind)
    {
        var buffer = s_pool.Rent();
        Log.BufferRented(kind, bufferCount: 1);
        return buffer;
    }

    public static byte[][] RentRange(JsonMemoryKind kind, int requiredChunks)
    {
        var chunks = s_chunkPool.Rent(requiredChunks);

        for (var i = 0; i < requiredChunks; i++)
        {
            chunks[i] = s_pool.Rent();
        }

        Log.BufferReturned(kind, requiredChunks);
        return chunks;
    }

    public static void Return(JsonMemoryKind kind, byte[] chunk)
    {
        s_pool.Return(chunk);
        Log.BufferReturned(kind, 1);
    }

    public static void Return(JsonMemoryKind kind, byte[][] chunks, int usedChunks)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(chunks);
#else
        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }
#endif

        foreach (var chunk in chunks.AsSpan(0, usedChunks))
        {
            s_pool.Return(chunk);
        }

        Log.BufferReturned(kind, usedChunks);
    }

    public static void Return(JsonMemoryKind kind, List<byte[]> chunks)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(chunks);

        foreach (var chunk in CollectionsMarshal.AsSpan(chunks))
        {
            s_pool.Return(chunk);
        }
#else
        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        foreach (var chunk in chunks)
        {
            s_pool.Return(chunk);
        }
#endif

        Log.BufferReturned(kind, chunks.Count);
    }
}
