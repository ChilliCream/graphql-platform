namespace HotChocolate.Fusion.Text.Json;

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
