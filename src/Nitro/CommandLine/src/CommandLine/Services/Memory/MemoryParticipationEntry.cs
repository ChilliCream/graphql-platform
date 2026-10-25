namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Distinguishes a curated memory from a journal entry in a participation result.
/// </summary>
internal enum MemoryParticipationKind
{
    Curated,
    Journal
}

/// <summary>
/// A curated memory or journal entry an agent created. A journal entry that was
/// promoted to a curated memory is represented once, as the curated memory; a
/// journal entry has a null <paramref name="Type"/> and empty <paramref name="Tags"/>.
/// </summary>
internal sealed record MemoryParticipationEntry(
    MemoryParticipationKind Kind,
    string Id,
    string? Type,
    IReadOnlyList<string> Tags,
    string Body,
    DateTimeOffset CreatedAt);
