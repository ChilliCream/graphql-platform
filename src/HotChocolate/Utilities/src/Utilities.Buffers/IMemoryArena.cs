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

    /// <summary>
    /// Rents a <see cref="MemorySegment"/> table of at least <paramref name="minLength"/> entries.
    /// The table's lifetime is bound to the arena: the arena reclaims it when it is disposed, so the
    /// caller must not return the table itself.
    /// </summary>
    /// <param name="minLength">
    /// The minimum number of entries the table must hold.
    /// </param>
    /// <returns>
    /// A <see cref="MemorySegment"/> array of at least <paramref name="minLength"/> entries.
    /// </returns>
    MemorySegment[] RentSegmentTable(int minLength);

    /// <summary>
    /// Grows the given <see cref="MemorySegment"/> table to twice its current length, copying the
    /// existing entries into the new table. The new table's lifetime is bound to the arena just like
    /// the original, so the caller must not return either table itself.
    /// </summary>
    /// <param name="table">
    /// The table to grow. On return it references the larger table.
    /// </param>
    void GrowSegmentTable(ref MemorySegment[] table);
}
