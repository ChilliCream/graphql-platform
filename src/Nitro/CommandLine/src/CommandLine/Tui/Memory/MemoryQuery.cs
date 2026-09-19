using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The pieces one memory tab search box entry parses into: free text passed
/// to <see cref="IMemoryStore.SearchCuratedAsync"/>, plus a type and tags.
/// </summary>
internal readonly record struct MemoryQuery(string Text, string? Type, IReadOnlyList<string> Tags)
{
    public bool IsEmpty => Text.Length == 0 && Type is null && Tags.Count == 0;
}
