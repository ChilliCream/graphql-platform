namespace HotChocolate.Fusion.Text.Json;

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

    public static void Return(byte[] chunk)
    {
    }

    public static void Return(List<byte[]> chunk)
    {
    }
}
