namespace HotChocolate.Buffers;

/// <summary>
/// Provides slabs of memory carved from a scope-bound backing buffer.
/// </summary>
public interface IMemoryArena
{
    /// <summary>
    /// Rents a slab of <paramref name="size"/> bytes.
    /// </summary>
    /// <param name="size">
    /// The number of bytes to rent.
    /// </param>
    /// <returns>
    /// A <see cref="MemorySegment"/> of exactly <paramref name="size"/> bytes.
    /// </returns>
    MemorySegment Rent(int size);
}
