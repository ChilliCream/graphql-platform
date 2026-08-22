namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

/// <summary>
/// Splits an explicit <c>--limit</c> across the curated and journal bands
/// under <c>--collection all</c> so a limit smaller than the curated band's
/// own count cannot starve the journal band entirely.
/// </summary>
internal static class MemoryBandLimit
{
    /// <summary>
    /// Returns the curated band's share (ceiling half) and the journal
    /// band's share (floor half) of <paramref name="limit"/>. Band order in
    /// output is unaffected: this only bounds how many entries each band's
    /// store query is allowed to return.
    /// </summary>
    public static (int Curated, int Journal) Split(int limit)
        => ((limit + 1) / 2, limit / 2);

    /// <summary>
    /// Grows the journal band's share by however many entries the curated
    /// band came up short of its own share, so the remainder flows to the
    /// journal band instead of going unused.
    /// </summary>
    public static int GrowJournalWithCuratedShortfall(int curatedShare, int curatedActualCount, int journalShare)
        => journalShare + (curatedShare - curatedActualCount);
}
