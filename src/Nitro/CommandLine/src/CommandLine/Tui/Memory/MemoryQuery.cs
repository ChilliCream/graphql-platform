using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// A parsed memory query with free text for <see cref="IMemoryStore"/> searches
/// and optional curated-memory type and tag filters.
/// </summary>
internal readonly record struct MemoryQuery(string Text, string? Type, IReadOnlyList<string> Tags)
{
    public bool IsEmpty => Text.Length == 0 && Type is null && Tags.Count == 0;
}
