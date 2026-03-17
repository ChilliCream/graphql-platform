using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Text.Json;

[EventSource(Name = "HotChocolate-Fusion-PathSegmentPool")]
internal sealed class PathSegmentPoolEventSource : EventSource
{
    public static readonly PathSegmentPoolEventSource Log = new();

    private PathSegmentPoolEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "Path segment pool created (PoolId={0}, SegmentSize={1}, Arrays={2}, TotalBytes={3})")]
    public void PoolCreated(int poolId, int segmentSize, int totalArrays, long totalBytes)
    {
        if (IsEnabled())
        {
            WriteEvent(1, poolId, segmentSize, totalArrays, totalBytes);
        }
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Verbose,
        Message = "Segment rented (ArrayId={0}, Length={1}, PoolId={2}, InUse={3})")]
    public void SegmentRented(int arrayId, int arrayLength, int poolId, int inUse)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(2, arrayId, arrayLength, poolId, inUse);
        }
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Verbose,
        Message = "Segment returned (ArrayId={0}, Length={1}, PoolId={2}, InUse={3})")]
    public void SegmentReturned(int arrayId, int arrayLength, int poolId, int inUse)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(3, arrayId, arrayLength, poolId, inUse);
        }
    }

    [Event(
        eventId: 4,
        Level = EventLevel.Warning,
        Message = "Path segment pool exhausted (PoolId={0}, MaxArrays={1})")]
    public void PoolExhausted(int poolId, int maxArrays)
    {
        if (IsEnabled())
        {
            WriteEvent(4, poolId, maxArrays);
        }
    }

    [Event(
        eventId: 5,
        Level = EventLevel.Informational,
        Message = "Segment dropped - pool full (ArrayId={0}, Length={1}, PoolId={2})")]
    public void SegmentDropped(int arrayId, int arrayLength, int poolId)
    {
        if (IsEnabled())
        {
            WriteEvent(5, arrayId, arrayLength, poolId);
        }
    }

    [Event(
        eventId: 6,
        Level = EventLevel.Informational,
        Message = "Segment allocated (ArrayId={0}, Length={1}, PoolId={2})")]
    public void SegmentAllocated(int arrayId, int arrayLength, int poolId)
    {
        if (IsEnabled())
        {
            WriteEvent(6, arrayId, arrayLength, poolId);
        }
    }

    [Event(
        eventId: 7,
        Level = EventLevel.Informational,
        Message = "Path segment pool trimmed (PoolId={0}, Trimmed={1}, Remaining={2}, InUse={3})")]
    public void PoolTrimmed(int poolId, int trimmed, int remaining, int inUse)
    {
        if (IsEnabled())
        {
            WriteEvent(7, poolId, trimmed, remaining, inUse);
        }
    }
}
