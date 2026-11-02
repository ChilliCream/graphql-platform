#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Diagnostics.Tracing;

namespace HotChocolate.Buffers;

/// <summary>
/// Emits a per-arena lifecycle ledger that correlates every rent, grow, seal and abandon to the
/// arena that performed it via a stable arena id. A listener can post-process the ledger to find
/// arenas that rented pooled memory or arrays and never returned them.
/// </summary>
[EventSource(Name = "HotChocolate-Buffers-MemoryArena")]
internal sealed class MemoryArenaEventSource : EventSource
{
    public static readonly MemoryArenaEventSource Log = new();

    private static volatile bool s_enabled;

    private MemoryArenaEventSource() { }

    /// <summary>
    /// Gets a value indicating whether a listener is currently subscribed to this source. The hot
    /// rental path reads this plain flag instead of calling <see cref="EventSource.IsEnabled()"/> so
    /// it adds only a single field read and a predictable not-taken branch when tracing is off.
    /// </summary>
    public static bool IsTracingEnabled => s_enabled;

    protected override void OnEventCommand(EventCommandEventArgs command)
    {
        if (command.Command == EventCommand.Enable)
        {
            s_enabled = true;
        }
        else if (command.Command == EventCommand.Disable)
        {
            s_enabled = false;
        }
    }

    [Event(
        eventId: 1,
        Level = EventLevel.Informational,
        Keywords = Keywords.Lifecycle,
        Message = "Arena created (ArenaId={0})")]
    public void ArenaCreated(long arenaId)
    {
        if (IsEnabled())
        {
            WriteEvent(1, arenaId);
        }
    }

    [Event(
        eventId: 2,
        Level = EventLevel.Verbose,
        Keywords = Keywords.Memory,
        Message = "Memory rented (ArenaId={0}, SizeInBytes={1})")]
    public void MemoryRented(long arenaId, int sizeInBytes)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Memory))
        {
            WriteEvent(2, arenaId, sizeInBytes);
        }
    }

    [Event(
        eventId: 3,
        Level = EventLevel.Verbose,
        Keywords = Keywords.Arrays,
        Message = "Array rented (ArenaId={0}, Length={1})")]
    public void ArrayRented(long arenaId, int length)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Arrays))
        {
            WriteEvent(3, arenaId, length);
        }
    }

    [Event(
        eventId: 4,
        Level = EventLevel.Verbose,
        Keywords = Keywords.Arrays,
        Message = "Array grown (ArenaId={0}, OldLength={1}, NewLength={2})")]
    public void ArrayGrown(long arenaId, int oldLength, int newLength)
    {
        if (IsEnabled(EventLevel.Verbose, Keywords.Arrays))
        {
            WriteEvent(4, arenaId, oldLength, newLength);
        }
    }

    [Event(
        eventId: 5,
        Level = EventLevel.Informational,
        Keywords = Keywords.Lifecycle,
        Message = "Arena sealed (ArenaId={0}, PagesReturned={1}, BytesReturned={2}, ArraysReturned={3}, "
            + "RentCount={4}, RentBytes={5})")]
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "The payload contains only primitive types, which the trimmer handles safely.")]
#endif
    public unsafe void ArenaSealed(
        long arenaId,
        int pagesReturned,
        long bytesReturned,
        int arraysReturned,
        long rentCount,
        long rentBytes)
    {
        if (IsEnabled())
        {
            var data = stackalloc EventData[6];
            data[0].DataPointer = (nint)(&arenaId);
            data[0].Size = sizeof(long);
            data[1].DataPointer = (nint)(&pagesReturned);
            data[1].Size = sizeof(int);
            data[2].DataPointer = (nint)(&bytesReturned);
            data[2].Size = sizeof(long);
            data[3].DataPointer = (nint)(&arraysReturned);
            data[3].Size = sizeof(int);
            data[4].DataPointer = (nint)(&rentCount);
            data[4].Size = sizeof(long);
            data[5].DataPointer = (nint)(&rentBytes);
            data[5].Size = sizeof(long);
            WriteEventCore(5, 6, data);
        }
    }

    [Event(
        eventId: 6,
        Level = EventLevel.Warning,
        Keywords = Keywords.Lifecycle,
        Message = "Arena abandoned (ArenaId={0}, PagesAbandoned={1}, BytesAbandoned={2}, "
            + "ArraysAbandoned={3}, FromFinalizer={4})")]
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification = "The payload contains only primitive types, which the trimmer handles safely.")]
#endif
    public unsafe void ArenaAbandoned(
        long arenaId,
        int pagesAbandoned,
        long bytesAbandoned,
        int arraysAbandoned,
        int fromFinalizer)
    {
        if (IsEnabled())
        {
            var data = stackalloc EventData[5];
            data[0].DataPointer = (nint)(&arenaId);
            data[0].Size = sizeof(long);
            data[1].DataPointer = (nint)(&pagesAbandoned);
            data[1].Size = sizeof(int);
            data[2].DataPointer = (nint)(&bytesAbandoned);
            data[2].Size = sizeof(long);
            data[3].DataPointer = (nint)(&arraysAbandoned);
            data[3].Size = sizeof(int);
            data[4].DataPointer = (nint)(&fromFinalizer);
            data[4].Size = sizeof(int);
            WriteEventCore(6, 5, data);
        }
    }

    [Event(
        eventId: 7,
        Level = EventLevel.Informational,
        Keywords = Keywords.Arrays,
        Message = "Segment table allocated (Length={0})")]
    public void TableAllocated(int length)
    {
        if (IsEnabled())
        {
            WriteEvent(7, length);
        }
    }

    [Event(
        eventId: 8,
        Level = EventLevel.Informational,
        Keywords = Keywords.Arrays,
        Message = "Segment table dropped (Length={0})")]
    public void TableDropped(int length)
    {
        if (IsEnabled())
        {
            WriteEvent(8, length);
        }
    }

    /// <summary>
    /// Keywords that group the arena lifecycle events so a listener can subscribe selectively.
    /// </summary>
    public static class Keywords
    {
        /// <summary>
        /// Arena creation, sealing and abandonment.
        /// </summary>
        public const EventKeywords Lifecycle = (EventKeywords)0x1;

        /// <summary>
        /// Backing page rentals from the memory pool.
        /// </summary>
        public const EventKeywords Memory = (EventKeywords)0x2;

        /// <summary>
        /// Segment table array rentals and growth.
        /// </summary>
        public const EventKeywords Arrays = (EventKeywords)0x4;
    }
}
