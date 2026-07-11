using System.Runtime.CompilerServices;

namespace HotChocolate.Types.Introspection;

/// <summary>
/// Provides text tokenization for the BM25 search index.
/// Handles camelCase/PascalCase splitting and non-alphanumeric boundary splitting.
/// </summary>
internal static class BM25Tokenizer
{
    /// <summary>
    /// Tokenizes the specified text into an array of lowercase tokens.
    /// </summary>
    /// <param name="text">
    /// The text to tokenize.
    /// </param>
    /// <returns>
    /// An array of lowercase tokens extracted from the text.
    /// </returns>
    public static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var tokens = new List<string>();
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (!char.IsLetterOrDigit(c))
            {
                // Non-alphanumeric boundary: emit token if accumulated.
                if (i > start)
                {
                    AddCamelCaseTokens(tokens, text, start, i);
                }

                start = i + 1;
                continue;
            }

            // Detect camelCase boundary: lowercase followed by uppercase.
            if (i > start && char.IsUpper(c) && char.IsLower(text[i - 1]))
            {
                AddCamelCaseTokens(tokens, text, start, i);
                start = i;
                continue;
            }

            // Detect PascalCase boundary: uppercase followed by uppercase then lowercase
            // e.g., "XMLParser" -> "XML", "Parser"
            if (i > start + 1
                && char.IsUpper(c)
                && char.IsUpper(text[i - 1])
                && i + 1 < text.Length
                && char.IsLower(text[i + 1]))
            {
                // Emit everything before the current uppercase as one token.
                // But only if the segment from start..i-1 hasn't already been split.
                // The segment start..(i-1) is the uppercase run, and 'i' starts a new word.
                if (i - 1 > start)
                {
                    EmitToken(tokens, text, start, i);
                }

                start = i;
            }
        }

        // Emit remaining token.
        if (start < text.Length)
        {
            AddCamelCaseTokens(tokens, text, start, text.Length);
        }

        return tokens.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddCamelCaseTokens(
        List<string> tokens,
        string text,
        int start,
        int end)
    {
        // For simple segments that have no further camelCase boundaries,
        // just emit the whole segment.
        EmitToken(tokens, text, start, end);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EmitToken(
        List<string> tokens,
        string text,
        int start,
        int end)
    {
        var length = end - start;

        // Filter out single-character tokens that are not meaningful.
        if (length <= 1)
        {
            return;
        }

        var token = text.Substring(start, length).ToLowerInvariant();
        tokens.Add(token);
    }
}
