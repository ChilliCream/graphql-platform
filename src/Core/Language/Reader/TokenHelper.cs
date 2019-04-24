using System;

namespace HotChocolate.Language
{
    internal static class TokenHelper
    {
        private static readonly bool[] _isString = new bool[22];
        private static readonly bool[] _isScalar = new bool[22];

        static TokenHelper()
        {
            _isString[(int)TokenKind.BlockString] = true;
            _isString[(int)TokenKind.String] = true;

            _isScalar[(int)TokenKind.BlockString] = true;
            _isScalar[(int)TokenKind.String] = true;
            _isScalar[(int)TokenKind.Integer] = true;
            _isScalar[(int)TokenKind.Float] = true;
        }

        public static bool IsDescription(ref Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        public static bool IsString(ref Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        public static bool IsScalarValue(ref Utf8GraphQLReader reader)
        {
            return _isScalar[(int)reader.Kind];
        }

        public static bool IsName(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Name;
        }

        public static bool IsAt(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.At;
        }

        public static bool IsDollar(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Dollar;
        }

        public static bool IsColon(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Colon;
        }

        public static bool IsLeftBrace(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.LeftBrace;
        }

        public static bool IsLeftParenthesis(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.LeftParenthesis;
        }

        public static bool IsSpread(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Spread;
        }
    }
}
