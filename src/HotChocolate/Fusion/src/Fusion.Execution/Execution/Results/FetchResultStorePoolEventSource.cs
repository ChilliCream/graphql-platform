using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Execution.Results;

[EventSource(Name = "HotChocolate-Fusion-FetchResultStorePool")]
internal sealed class FetchResultStorePoolEventSource : EventSource
{
    public static readonly FetchResultStorePoolEventSource Log = new();

    private FetchResultStorePoolEventSource() { }

    [Event(1, Level = EventLevel.Verbose, Message = "Store rented from pool (hit)")]
    public void StoreHit()
    {
        if (IsEnabled())
        {
            WriteEvent(1);
        }
    }

    [Event(2, Level = EventLevel.Informational, Message = "Pool empty, new store allocated (miss)")]
    public void StoreMiss()
    {
        if (IsEnabled())
        {
            WriteEvent(2);
        }
    }

    [Event(3, Level = EventLevel.Warning, Message = "Pool full, store disposed on return (dropped)")]
    public void StoreDropped()
    {
        if (IsEnabled())
        {
            WriteEvent(3);
        }
    }

    [Event(4, Level = EventLevel.Informational, Message = "Pool trimmed to level {0} (limit={1})")]
    public void PoolTrimmed(int level, int limit)
    {
        if (IsEnabled())
        {
            WriteEvent(4, level, limit);
        }
    }
}
