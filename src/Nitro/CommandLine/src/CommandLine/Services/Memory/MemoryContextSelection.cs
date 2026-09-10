namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The entries a <c>context</c> request admitted, and, when the very first
/// candidate alone exceeded the character budget, that candidate's id so
/// the caller can report it as omitted rather than silently returning
/// nothing.
/// </summary>
internal sealed record MemoryContextSelection(
    IReadOnlyList<MemoryRecord> Entries, string? OmittedEntryId);
