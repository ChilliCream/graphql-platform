namespace HotChocolate.Buffers;

/// <summary>
/// An <see cref="IMemoryArenaSource"/> that returns the same <see cref="IMemoryArena"/> on every call.
/// Use it for a request-scoped stream of results whose documents all share one arena, for example a
/// query or mutation, an Apollo-style request batch, or an incremental delivery stream.
/// </summary>
public sealed class FixedMemoryArenaSource : IMemoryArenaSource
{
    private IMemoryArena _arena;

    /// <summary>
    /// Initializes a new instance of <see cref="FixedMemoryArenaSource"/>.
    /// </summary>
    /// <param name="arena">
    /// The arena returned by every <see cref="GetNextArena"/> call.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arena"/> is <see langword="null"/>.
    /// </exception>
    public FixedMemoryArenaSource(IMemoryArena arena)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(arena);
#else
        if (arena is null)
        {
            throw new ArgumentNullException(nameof(arena));
        }
#endif
        _arena = arena;
    }

    /// <inheritdoc />
    public IMemoryArena GetNextArena() => _arena;

    /// <summary>
    /// Points this source at <paramref name="arena"/> so the same instance can be reused across
    /// scopes without allocating a new source.
    /// </summary>
    /// <param name="arena">
    /// The arena returned by every subsequent <see cref="GetNextArena"/> call.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arena"/> is <see langword="null"/>.
    /// </exception>
    internal void Reset(IMemoryArena arena)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(arena);
#else
        if (arena is null)
        {
            throw new ArgumentNullException(nameof(arena));
        }
#endif
        _arena = arena;
    }
}
