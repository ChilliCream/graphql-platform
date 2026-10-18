using System.Collections.Concurrent;

namespace HotChocolate.Buffers;

/// <summary>
/// A bounded pool of <see cref="MemoryArena"/> instances. An arena rented from the pool returns
/// itself to the pool when it is disposed after being sealed.
/// </summary>
internal sealed class MemoryArenaPool
{
    private readonly ConcurrentQueue<MemoryArena> _arenas = new();
    private readonly int _maximumRetained;
    private int _count;

    /// <summary>
    /// Initializes a new instance of <see cref="MemoryArenaPool"/>.
    /// </summary>
    /// <param name="maximumRetained">The maximum number of idle arenas the pool keeps.</param>
    public MemoryArenaPool(int maximumRetained)
    {
#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumRetained);
#else
        if (maximumRetained <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRetained));
        }
#endif

        _maximumRetained = maximumRetained;
    }

    /// <summary>
    /// Gets the shared arena pool.
    /// </summary>
    public static MemoryArenaPool Shared { get; } = new(256);

    /// <summary>
    /// Rents an open arena.
    /// </summary>
    public MemoryArena Rent()
    {
        if (_arenas.TryDequeue(out var arena))
        {
            Interlocked.Decrement(ref _count);
            arena.OnRented();
            return arena;
        }

        return new MemoryArena(this);
    }

    internal bool TryReturn(MemoryArena arena)
    {
        if (Interlocked.Increment(ref _count) > _maximumRetained)
        {
            Interlocked.Decrement(ref _count);
            return false;
        }

        _arenas.Enqueue(arena);
        return true;
    }
}
