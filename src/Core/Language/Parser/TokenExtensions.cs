using System;

namespace HotChocolate.Language
{
    internal static class TokenExtensions
    {
        private static readonly bool[] _isString = new bool[22];
        private static readonly bool[] _isScalar = new bool[22];

        static TokenExtensions()
        {
            _isString[(int)TokenKind.BlockString] = true;
            _isString[(int)TokenKind.String] = true;

            _isScalar[(int)TokenKind.BlockString] = true;
            _isScalar[(int)TokenKind.String] = true;
            _isScalar[(int)TokenKind.Integer] = true;
            _isScalar[(int)TokenKind.Float] = true;
        }


        public static bool IsDescription(this SyntaxToken token)
        {
            return _isString[(int)token.Kind];
        }

        public static bool IsString(this SyntaxToken token)
        {
            return _isString[(int)token.Kind];
        }

        public static bool IsScalarValue(this SyntaxToken token)
        {
            return _isScalar[(int)token.Kind];
        }

        public static bool IsName(this SyntaxToken token)
        {
            return token.Kind == TokenKind.Name;
        }

        public static bool IsAt(this SyntaxToken token)
        {
            return token.Kind == TokenKind.At;
        }

        public static bool IsDollar(this SyntaxToken token)
        {
            return token.Kind == TokenKind.Dollar;
        }

        public static bool IsColon(this SyntaxToken token)
        {
            return token.Kind == TokenKind.Colon;
        }

        public static bool IsLeftBrace(this SyntaxToken token)
        {
            return token.Kind == TokenKind.LeftBrace;
        }

        public static bool IsLeftParenthesis(this SyntaxToken token)
        {
            return token.Kind == TokenKind.LeftParenthesis;
        }

        public static bool IsSpread(this SyntaxToken token)
        {
            return token.Kind == TokenKind.Spread;
        }

        public static bool IsOnKeyword(this SyntaxToken token)
        {
            return token.Kind == TokenKind.Name
                && token.Value == Keywords.On;
        }

        public static SyntaxToken Peek(this SyntaxToken token)
        {
            if (token.Kind == TokenKind.EndOfFile)
            {
                throw new SyntaxException(token,
                    "The specified token is the last " +
                    "token in the token chain.");
            }

            SyntaxToken next = token;

            do
            {
                next = next.Next;
            }
            while (next.Kind == TokenKind.Comment);

            return next;
        }
    }
}
