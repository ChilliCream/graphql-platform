using System;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDescription(ref Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(ref Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsScalarValue(ref Utf8GraphQLReader reader)
        {
            return _isScalar[(int)reader.Kind];
        }

        public static bool IsName(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAt(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.At;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDollar(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Dollar;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsColon(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Colon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSpread(ref Utf8GraphQLReader reader)
        {
            return reader.Kind == TokenKind.Spread;
        }
    }
}
