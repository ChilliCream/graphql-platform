using HotChocolate.Buffers;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;

internal readonly ref struct SseEventData(
    SseEventType type,
    byte[][]? data,
    int lastChunkSize)
{
    public readonly SseEventType Type = type;
    public readonly byte[][]? Data = data;
    public readonly int LastChunkSize = lastChunkSize;
}

#else
namespace HotChocolate.Transport.Http;

internal readonly ref struct SseEventData(
    SseEventType type,
    PooledArrayWriter? data)
{
    public readonly SseEventType Type = type;
    public readonly PooledArrayWriter? Data = data;
}
#endif
