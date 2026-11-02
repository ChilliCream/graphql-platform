using System.Diagnostics.Tracing;

namespace HotChocolate.Fusion.Execution;

[EventSource(Name = "HotChocolate-Fusion-OperationPlanContextPool")]
internal sealed class OperationPlanContextPoolEventSource : EventSource
{
    public static readonly OperationPlanContextPoolEventSource Log = new();

    private OperationPlanContextPoolEventSource() { }

    [Event(1, Level = EventLevel.Verbose, Message = "Context rented from pool (hit)")]
    public void ContextHit()
    {
        if (IsEnabled())
        {
            WriteEvent(1);
        }
    }

    [Event(2, Level = EventLevel.Informational, Message = "Pool empty, new context allocated (miss)")]
    public void ContextMiss()
    {
        if (IsEnabled())
        {
            WriteEvent(2);
        }
    }

    [Event(3, Level = EventLevel.Warning, Message = "Pool full, context disposed on return (dropped)")]
    public void ContextDropped()
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
