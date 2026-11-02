using System.Diagnostics.Tracing;

#if FUSION
namespace HotChocolate.Fusion.Text.Json;

[EventSource(Name = "HotChocolate-Fusion-MetaDb")]
#else
namespace HotChocolate.Text.Json;

[EventSource(Name = "HotChocolate-MetaDb")]
#endif
internal sealed class MetaDbEventSource : EventSource
{
    public static readonly MetaDbEventSource Log = new();

    private MetaDbEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "MetaDb created (DbId={0}, EstimatedRows={1}, ChunksAllocated={2})")]
    public void MetaDbCreated(int dbId, int estimatedRows, int chunksAllocated)
    {
        if (IsEnabled())
        {
            WriteEvent(1, dbId, estimatedRows, chunksAllocated);
        }
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Informational,
        Message = "MetaDb disposed (DbId={0}, ChunksUsed={1}, RowsAllocated={2})")]
    public void MetaDbDisposed(int dbId, int chunksUsed, int rowsAllocated)
    {
        if (IsEnabled())
        {
            WriteEvent(2, dbId, chunksUsed, rowsAllocated);
        }
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Verbose,
        Message = "MetaDb chunk allocated (DbId={0}, ChunkIndex={1})")]
    public void ChunkAllocated(int dbId, int chunkIndex)
    {
        if (IsEnabled())
        {
            WriteEvent(3, dbId, chunkIndex);
        }
    }

    [Event(
        eventId: 4,
        Level = EventLevel.Warning,
        Message = "MetaDb chunks expanded (DbId={0}, OldCapacity={1}, NewCapacity={2})")]
    public void ChunksExpanded(int dbId, int oldCapacity, int newCapacity)
    {
        if (IsEnabled())
        {
            WriteEvent(4, dbId, oldCapacity, newCapacity);
        }
    }
}
