namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The memory kind the Memory tab's table currently shows: every entry, curated memories
/// only, or journal entries only. Also tags a <see cref="MemoryRow"/> by its own kind, where
/// it is always <see cref="Curated"/> or <see cref="Journal"/>, never <see cref="All"/>.
/// </summary>
internal enum MemoryCollectionFilter
{
    All,
    Curated,
    Journal
}
