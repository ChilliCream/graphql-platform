namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The memory collections a read can target: the curated store, the journal,
/// or both.
/// </summary>
internal static class MemoryCollections
{
    public const string Curated = "curated";
    public const string Journal = "journal";
    public const string All = "all";
}
