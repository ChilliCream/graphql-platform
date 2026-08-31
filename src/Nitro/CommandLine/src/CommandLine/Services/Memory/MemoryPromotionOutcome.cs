namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The result of promoting a journal entry: the curated memory, and whether
/// it already existed from an earlier promotion of the same journal entry.
/// </summary>
internal sealed record MemoryPromotionOutcome(MemoryRecord Record, bool AlreadyPromoted);
