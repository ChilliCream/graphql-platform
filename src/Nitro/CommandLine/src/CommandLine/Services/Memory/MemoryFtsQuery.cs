namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Builds an FTS5 expression requiring each whitespace-separated word as a quoted
/// phrase, without treating input as query syntax.
/// </summary>
internal static class MemoryFtsQuery
{
    public static string BuildLiteralMatch(string text)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(QuoteWord));
    }

    private static string QuoteWord(string word) => "\"" + word.Replace("\"", "\"\"") + "\"";
}
