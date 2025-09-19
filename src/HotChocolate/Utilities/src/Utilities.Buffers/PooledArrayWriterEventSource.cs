using System.Diagnostics.Tracing;

namespace HotChocolate.Buffers;

/// <summary>
/// EventSource for monitoring PooledArrayWriter performance and buffer operations.
/// </summary>
[EventSource(Name = "HotChocolate-Buffers-PooledArrayWriter")]
internal sealed class PooledArrayWriterEventSource : EventSource
{
    public static readonly PooledArrayWriterEventSource Log = new();

    private PooledArrayWriterEventSource() { }

    [Event(1, Level = EventLevel.Informational)]
    public void WriterCreated(int initialBufferSize, int actualCapacity)
    {
        WriteEvent(1, initialBufferSize, actualCapacity);
    }

    [Event(2, Level = EventLevel.Warning)]
    public void BufferResize(int oldSize, int newSize, int dataLength, int requestedSize, int resizeCount)
    {
        WriteEvent(2, oldSize, newSize, dataLength, requestedSize, resizeCount);
    }

    [Event(3, Level = EventLevel.Verbose)]
    public void MemoryRequested(int sizeHint, int actualSize, bool resizeRequired)
    {
        WriteEvent(3, sizeHint, actualSize, resizeRequired);
    }

    [Event(4, Level = EventLevel.Verbose)]
    public void WriterAdvanced(int count, int newPosition, int remainingCapacity)
    {
        WriteEvent(4, count, newPosition, remainingCapacity);
    }

    [Event(5, Level = EventLevel.Informational)]
    public void WriterReset(int previousLength, int capacity)
    {
        WriteEvent(5, previousLength, capacity);
    }

    [Event(6, Level = EventLevel.Informational)]
    public void WriterDisposed(int finalLength, int capacity, int totalResizes)
    {
        WriteEvent(6, finalLength, capacity, totalResizes);
    }

    [Event(7, Level = EventLevel.Error)]
    public void BufferOverflow(int requestedCount, int availableCapacity)
    {
        WriteEvent(7, requestedCount, availableCapacity);
    }

    [Event(8, Level = EventLevel.Warning)]
    public void LargeAllocation(int size, int threshold)
    {
        WriteEvent(8, size, threshold);
    }
}
