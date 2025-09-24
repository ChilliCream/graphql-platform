namespace HotChocolate.Fusion.Text.Json;

internal static class MetaDbMemory
{
    public const int ChunkSize = RowsPerChunk * CompositeResultDocument.DbRow.Size;
    public const int RowsPerChunk = 6552;

    public static byte[] Rent() => new byte[ChunkSize];

    public static void Return(byte[] chunk)
    {
    }
}
