#if FUSION
namespace HotChocolate.Fusion.Transport.Http;

internal readonly ref struct SseEventData(
    SseEventType type,
    byte[][]? data,
    int lastChunkSize,
    int usedChunks)
{
    public readonly SseEventType Type = type;
    public readonly byte[][]? Data = data;
    public readonly int LastChunkSize = lastChunkSize;
    public readonly int UsedChunks = usedChunks;
}

#else
using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal readonly ref struct SseEventData(
    SseEventType type,
    PooledArrayWriter? data)
{
    public readonly SseEventType Type = type;
    public readonly PooledArrayWriter? Data = data;
}
#endif
