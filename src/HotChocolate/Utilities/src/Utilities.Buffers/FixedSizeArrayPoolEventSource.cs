using System.Diagnostics.Tracing;

namespace HotChocolate.Buffers;

[EventSource(Name = "HotChocolate-Buffers-FixedSizeArrayPool")]
internal sealed class FixedSizeArrayPoolEventSource : EventSource
{
    public static readonly FixedSizeArrayPoolEventSource Log = new();

    private FixedSizeArrayPoolEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "MetaDb pool created (PoolId={0}, Chunks={1}, TotalBytes={2})")]
    public void PoolCreated(int poolId, int totalChunks, long totalBytes)
    {
        if (IsEnabled())
        {
            WriteEvent(1, poolId, totalChunks, totalBytes);
        }
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Verbose,
        Message = "Buffer rented (BufferId={0}, Size={1}, PoolId={2}, InUse={3})")]
    public void BufferRented(int bufferId, int bufferSize, int poolId, int inUse)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(2, bufferId, bufferSize, poolId, inUse);
        }
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Verbose,
        Message = "Buffer returned (BufferId={0}, Size={1}, PoolId={2}, InUse={3})")]
    public void BufferReturned(int bufferId, int bufferSize, int poolId, int inUse)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(3, bufferId, bufferSize, poolId, inUse);
        }
    }

    [Event(
        eventId: 4,
        Level = EventLevel.Warning,
        Message = "Pool exhausted! (PoolId={0}, MaxChunks={1})")]
    public void PoolExhausted(int poolId, int maxChunks)
    {
        if (IsEnabled())
        {
            WriteEvent(4, poolId, maxChunks);
        }
    }

    [Event(
        eventId: 5,
        Level = EventLevel.Informational,
        Message = "Buffer dropped - pool full (BufferId={0}, Size={1}, PoolId={2})")]
    public void BufferDropped(int bufferId, int bufferSize, int poolId)
    {
        if (IsEnabled())
        {
            WriteEvent(5, bufferId, bufferSize, poolId);
        }
    }

    [Event(
        eventId: 6,
        Level = EventLevel.Informational,
        Message = "Buffer allocated (BufferId={0}, Size={1}, PoolId={2})")]
    public void BufferAllocated(int bufferId, int bufferSize, int poolId)
    {
        if (IsEnabled())
        {
            WriteEvent(6, bufferId, bufferSize, poolId);
        }
    }
}
