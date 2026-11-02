namespace HotChocolate.Text.Json;

/// <summary>
/// Identifies the chunk-size bucket a result document allocates its memory slabs in.
/// </summary>
/// <remarks>
/// The byte size of a bucket is <c>1 &lt;&lt; (10 + (int)ordinal)</c>, so the enum spans
/// 1 KB (<see cref="Size1K"/>) through 128 KB (<see cref="Size128K"/>) and fits the three
/// size bits of a cursor.
/// </remarks>
internal enum ChunkSize
{
    Size1K,
    Size2K,
    Size4K,
    Size8K,
    Size16K,
    Size32K,
    Size64K,
    Size128K
}
