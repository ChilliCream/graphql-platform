using System.Diagnostics.Tracing;

namespace HotChocolate.Buffers;

[EventSource(Name = "HotChocolate-Buffers-JsonMemory")]
internal sealed class JsonMemoryEventSource : EventSource
{
    public static readonly JsonMemoryEventSource Log = new();

    private JsonMemoryEventSource() { }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Message = "Pool reconfigured.")]
    public void ReconfiguredPool()
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(1);
        }
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Verbose,
        Message = "Buffer rented (Kind={0}, BufferCount={1})")]
    public void BufferRented(JsonMemoryKind kind, int bufferCount)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(2, kind, bufferCount);
        }
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Verbose,
        Message = "Buffer returned (Kind={0}, BufferCount={1})")]
    public void BufferReturned(JsonMemoryKind kind, int bufferCount)
    {
        if (IsEnabled(EventLevel.Verbose, EventKeywords.None))
        {
            WriteEvent(3, kind, bufferCount);
        }
    }
}
