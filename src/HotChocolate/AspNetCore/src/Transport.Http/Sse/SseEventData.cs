using HotChocolate.Buffers;

namespace HotChocolate.Transport.Http;

internal ref struct SseEventData(
    SseEventType type,
    PooledArrayWriter? data)
{
    public SseEventType Type = type;
    public PooledArrayWriter? Data = data;
}
