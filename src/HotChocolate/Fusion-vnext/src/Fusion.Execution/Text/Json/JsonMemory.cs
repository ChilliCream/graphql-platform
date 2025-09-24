namespace HotChocolate.Fusion.Text.Json;

// TODO : Implement
/// <summary>
/// Manages the memory for storing JSON data.
/// </summary>
internal static class JsonMemory
{
    /// <summary>
    /// The size of one JSON chunk.
    /// </summary>
    public const int ChunkSize = 128 * 1024;

    public static byte[] Rent() => new byte[ChunkSize];

    public static byte[][] RentRange(int requiredChunks)
    {
        var chunks = new byte[requiredChunks][];

        // Pre-allocate exactly the chunks we need
        for (var i = 0; i < requiredChunks; i++)
        {
            chunks[i]  =new byte[ChunkSize];
        }

        return chunks;
    }

    public static void Return(byte[] chunk)
    {
    }

    public static void Return(List<byte[]> chunk)
    {
    }

    public static void Return(byte[][] chunk)
    {
    }
}
