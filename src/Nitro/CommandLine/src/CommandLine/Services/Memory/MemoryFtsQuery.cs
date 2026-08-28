namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Turns raw search text into an FTS5 MATCH expression that matches it
/// literally: <c>search</c> never exposes FTS5 query syntax (operators like
/// <c>OR</c>, <c>NOT</c>, <c>NEAR</c>, or a bare <c>*</c>) to the caller, so
/// every whitespace-separated word becomes its own quoted phrase, implicitly
/// ANDed by FTS5's default token-to-token operator.
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
