namespace Prometheus.Language
{
    public static class TokenExtensions
    {
        public static bool IsDescription(this Token token)
        {
            return token.Kind == TokenKind.BlockString
                || token.Kind == TokenKind.String;
        }

        public static bool IsName(this Token token)
        {
            return token.Kind == TokenKind.Name;
        }

        public static bool IsAt(this Token token)
        {
            return token.Kind == TokenKind.At;
        }

        public static bool IsColon(this Token token)
        {
            return token.Kind == TokenKind.Colon;
        }

        public static bool IsLeftBrace(this Token token)
        {
            return token.Kind == TokenKind.LeftBrace;
        }

        public static bool IsLeftParenthesis(this Token token)
        {
            return token.Kind == TokenKind.LeftParenthesis;
        }

        public static bool IsRightBracket(this Token token)
        {
            return token.Kind == TokenKind.RightBracket;
        }
    }


}