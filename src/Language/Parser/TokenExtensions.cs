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


        public static bool IsDescription(this Token token)
        {
            return _isString[(int)token.Kind];
        }

        public static bool IsString(this Token token)
        {
            return _isString[(int)token.Kind];
        }

        public static bool IsScalarValue(this Token token)
        {
            return _isScalar[(int)token.Kind];
        }

        public static bool IsName(this Token token)
        {
            return token.Kind == TokenKind.Name;
        }

        public static bool IsAt(this Token token)
        {
            return token.Kind == TokenKind.At;
        }

        public static bool IsDollar(this Token token)
        {
            return token.Kind == TokenKind.Dollar;
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

        public static bool IsSpread(this Token token)
        {
            return token.Kind == TokenKind.Spread;
        }

        public static bool IsOnKeyword(this Token token)
        {
            return token.Kind == TokenKind.Name
                && token.Value == Keywords.On;
        }

        public static Token Peek(this Token token)
        {
            if (token.Kind == TokenKind.EndOfFile)
            {
                throw new InvalidOperationException(
                    "The specified token is the last token in the token chain.");
            }

            Token next = token;

            do
            {
                next = next.Next;
            }
            while (next.Kind == TokenKind.Comment);

            return next;
        }
    }
}