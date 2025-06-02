namespace HotChocolate.Language;

internal static class TokenPrinter
{
    private static readonly Dictionary<TokenKind, string> s_cachedStrings =
        new()
        {
            { TokenKind.StartOfFile, "<SOF>" },
            { TokenKind.EndOfFile, "<EOF>" },
            { TokenKind.Bang, "!" },
            { TokenKind.QuestionMark, "?" },
            { TokenKind.Dollar, "$" },
            { TokenKind.Ampersand, "&" },
            { TokenKind.LeftParenthesis, "(" },
            { TokenKind.RightParenthesis, ")" },
            { TokenKind.Spread, "..." },
            { TokenKind.Colon, ":" },
            { TokenKind.Equal, "=" },
            { TokenKind.At, "@" },
            { TokenKind.LeftBracket, "[" },
            { TokenKind.RightBracket, "]" },
            { TokenKind.LeftBrace, "{" },
            { TokenKind.RightBrace, "}" },
            { TokenKind.Pipe, "|" },
            { TokenKind.Name, "Name" },
            { TokenKind.Integer, "Int" },
            { TokenKind.Float, "Float" },
            { TokenKind.String, "String" },
            { TokenKind.BlockString, "BlockString" },
            { TokenKind.Comment, "Comment" },
            { TokenKind.Dot, "." },
        };

    public static string Print(ref Utf8GraphQLReader reader)
        => s_cachedStrings[reader.Kind];

    public static string Print(TokenKind tokenKind)
        => s_cachedStrings[tokenKind];
}
