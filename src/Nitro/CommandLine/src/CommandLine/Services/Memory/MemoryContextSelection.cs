namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Entries admitted to a context request, in input order.
/// <see cref="OmittedEntryId"/> identifies a first entry that exceeds the character
/// budget and is null otherwise.
/// </summary>
internal sealed record MemoryContextSelection(
    IReadOnlyList<MemoryRecord> Entries, string? OmittedEntryId);
