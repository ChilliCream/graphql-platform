namespace HotChocolate.Buffers;

/// <summary>
/// Provides the <see cref="IMemoryArena"/> that backs each document produced by a stream of results.
/// A single result takes an <see cref="IMemoryArena"/>; a stream of results takes an
/// <see cref="IMemoryArenaSource"/> and asks it for the arena that backs the next document.
/// </summary>
public interface IMemoryArenaSource
{
    /// <summary>
    /// Gets the <see cref="IMemoryArena"/> that backs the next document in the stream.
    /// </summary>
    /// <returns>
    /// The <see cref="IMemoryArena"/> to use for the next document.
    /// </returns>
    IMemoryArena GetNextArena();
}
