namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The fields needed to capture a new journal entry.
/// </summary>
internal sealed record MemoryJournalEntryCreation
{
    public required string Text { get; init; }
    public required string Actor { get; init; }
}
