using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// An <see cref="IMemoryArenaSource"/> that hands out a fresh <see cref="IMemoryArena"/> for every
/// subscription event so that each event document is backed by its own arena.
/// </summary>
/// <remarks>
/// This source has a strict lock-step contract: there is a single consumer, no prefetch, and
/// <see cref="Arena"/> pairs with the most recently yielded document. The consumer must read
/// <see cref="Arena"/> before calling <c>MoveNextAsync</c> again, because the next call replaces it
/// with the arena of the following event.
/// </remarks>
internal sealed class SubscriptionArenaSource : IMemoryArenaSource
{
    /// <summary>
    /// Gets the arena that backs the most recently produced document. Pairs with the most recently
    /// yielded event and must be read before the next document is requested.
    /// </summary>
    public IMemoryArena Arena { get; private set; } = default!;

    /// <inheritdoc />
    public IMemoryArena GetNextArena()
    {
        Arena = new MemoryArena();
        return Arena;
    }
}
