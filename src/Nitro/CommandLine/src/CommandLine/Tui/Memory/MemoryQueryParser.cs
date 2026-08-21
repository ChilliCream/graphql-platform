using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// The pieces one memory tab search box entry parses into: free text passed
/// to <see cref="IMemoryStore.SearchCuratedAsync"/> literally, plus a type
/// and tags narrowed the same way the CLI's <c>--type</c> and <c>--tag</c>
/// options do.
/// </summary>
internal readonly record struct MemoryQuery(string Text, string? Type, IReadOnlyList<string> Tags)
{
    public bool IsEmpty => Text.Length == 0 && Type is null && Tags.Count == 0;
}

/// <summary>
/// Parses the memory tab's single search box into a <see cref="MemoryQuery"/>:
/// whitespace-separated words starting with <c>tag:</c> or <c>type:</c>
/// narrow the curated list the same way the CLI's <c>--tag</c> (repeatable)
/// and <c>--type</c> (last one wins) options do; every other word joins the
/// free-text query passed to the store's own literal lexical search, so no
/// second query syntax is invented beyond these two recognized prefixes.
/// </summary>
internal static class MemoryQueryParser
{
    private const string TagPrefix = "tag:";
    private const string TypePrefix = "type:";

    public static MemoryQuery Parse(string input)
    {
        var words = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var textWords = new List<string>(words.Length);
        var tags = new List<string>();
        string? type = null;

        foreach (var word in words)
        {
            if (word.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase) && word.Length > TagPrefix.Length)
            {
                tags.Add(MemoryTags.Normalize(word[TagPrefix.Length..]));
            }
            else if (word.StartsWith(TypePrefix, StringComparison.OrdinalIgnoreCase) && word.Length > TypePrefix.Length)
            {
                type = MemoryTypes.Normalize(word[TypePrefix.Length..]);
            }
            else
            {
                textWords.Add(word);
            }
        }

        return new MemoryQuery(string.Join(' ', textWords), type, tags);
    }
}
