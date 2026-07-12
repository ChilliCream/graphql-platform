namespace HotChocolate.Buffers;

/// <summary>
/// A test arena source that always hands out the same arena. Used by transport reader tests that
/// need an <see cref="IMemoryArenaSource"/> over a single fixed arena.
/// </summary>
internal sealed class FixedMemoryArenaSource(IMemoryArena arena) : IMemoryArenaSource
{
    public IMemoryArena GetNextArena() => arena;
}
