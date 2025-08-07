using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal readonly ref struct SseEventData(
    SseEventType type,
    PooledArrayWriter? data)
{
    public readonly SseEventType Type = type;
    public readonly PooledArrayWriter? Data = data;
}
