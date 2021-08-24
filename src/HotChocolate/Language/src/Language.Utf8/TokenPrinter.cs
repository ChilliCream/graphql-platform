using System.Collections.Generic;

namespace HotChocolate.Language
{
    internal static class TokenPrinter
    {
        private static readonly Dictionary<TokenKind, string> _cachedStrings =
            new()
            {
                {TokenKind.StartOfFile, "<SOF>"},
                {TokenKind.EndOfFile, "<EOF>"},
                {TokenKind.Bang, "!"},
                {TokenKind.Dollar, "$"},
                {TokenKind.Ampersand, "&"},
                {TokenKind.LeftParenthesis, "("},
                {TokenKind.RightParenthesis, ")"},
                {TokenKind.Spread, "..."},
                {TokenKind.Colon, ":"},
                {TokenKind.Equal, "="},
                {TokenKind.At, "@"},
                {TokenKind.LeftBracket, "["},
                {TokenKind.RightBracket, "]"},
                {TokenKind.LeftBrace, "{"},
                {TokenKind.RightBrace, "}"},
                {TokenKind.Pipe, "|"},
                {TokenKind.Name, "Name"},
                {TokenKind.Integer, "Int"},
                {TokenKind.Float, "Float"},
                {TokenKind.String, "String"},
                {TokenKind.BlockString, "BlockString"},
                {TokenKind.Comment, "Comment"}
            };

        public static string Print(in Utf8GraphQLReader reader)
            =>  _cachedStrings[reader.Kind];

        public static string Print(TokenKind tokenKind)
            => _cachedStrings[tokenKind];
    }
}
