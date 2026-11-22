namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Identifies the chunk-size bucket a document allocates its memory slabs at.
/// The ordinal maps to a byte size via <c>1 &lt;&lt; (10 + ordinal)</c>, so the
/// bucket fits the three size bits of a <c>Cursor</c>.
/// </summary>
internal enum ChunkSize
{
    /// <summary>1 KB chunks.</summary>
    Size1K,

    /// <summary>2 KB chunks.</summary>
    Size2K,

    /// <summary>4 KB chunks.</summary>
    Size4K,

    /// <summary>8 KB chunks.</summary>
    Size8K,

    /// <summary>16 KB chunks.</summary>
    Size16K,

    /// <summary>32 KB chunks.</summary>
    Size32K,

    /// <summary>64 KB chunks.</summary>
    Size64K,

    /// <summary>128 KB chunks.</summary>
    Size128K
}

/// <summary>
/// The sizes of the chunks of a result document by chunk index.
/// </summary>
internal static class ChunkSchedule
{
    /// <summary>
    /// The index of the first chunk of the constant tail. This chunk and every later chunk has
    /// the size <see cref="ChunkSize.Size128K"/>.
    /// </summary>
    public const int RampLength = 18;

    private const int First32K = 12;
    private const int First64K = 16;

    /// <summary>
    /// Gets the chunk size of the chunk at the given index.
    /// </summary>
    /// <remarks>
    /// The sizes are 1, 2, 4 and 8 KB, eight chunks of 16 KB, four of 32 KB, two of 64 KB, and 128 KB
    /// from the chunk at <see cref="RampLength"/> on.
    /// </remarks>
    public static ChunkSize SizeFor(int chunkIndex)
    {
        if (chunkIndex <= (int)ChunkSize.Size16K)
        {
            return (ChunkSize)chunkIndex;
        }

        if (chunkIndex < First32K)
        {
            return ChunkSize.Size16K;
        }

        if (chunkIndex < First64K)
        {
            return ChunkSize.Size32K;
        }

        return chunkIndex < RampLength ? ChunkSize.Size64K : ChunkSize.Size128K;
    }
}
