namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The memory collections a read can target: the curated store, the journal,
/// or both. The journal collection is always empty until the journal
/// capture slice lands.
/// </summary>
internal static class MemoryCollections
{
    public const string Curated = "curated";
    public const string Journal = "journal";
    public const string All = "all";
}
