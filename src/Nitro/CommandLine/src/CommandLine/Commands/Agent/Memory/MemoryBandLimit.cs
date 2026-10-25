namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Memory;

/// <summary>
/// Splits an explicit <c>--limit</c> across the curated and journal bands
/// under <c>--collection all</c>.
/// </summary>
internal static class MemoryBandLimit
{
    /// <summary>
    /// Returns the curated band's share (ceiling half) and the journal
    /// band's share (floor half) of <paramref name="limit"/>.
    /// </summary>
    public static (int Curated, int Journal) Split(int limit)
        => ((limit + 1) / 2, limit / 2);

    /// <summary>
    /// Grows the journal band's share by however many entries the curated
    /// band came up short of its own share.
    /// </summary>
    public static int GrowJournalWithCuratedShortfall(int curatedShare, int curatedActualCount, int journalShare)
        => journalShare + (curatedShare - curatedActualCount);

    /// <summary>
    /// Returns the curated band's new share after the journal band came up
    /// short of its own (possibly already grown) share.
    /// </summary>
    public static int GrowCuratedWithJournalShortfall(int explicitLimit, int journalActualCount)
        => explicitLimit - journalActualCount;
}
