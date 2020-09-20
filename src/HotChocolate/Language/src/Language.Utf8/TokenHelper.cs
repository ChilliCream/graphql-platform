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
        public static bool IsDescription(in Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsString(in Utf8GraphQLReader reader)
        {
            return _isString[(int)reader.Kind];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsScalarValue(in Utf8GraphQLReader reader)
        {
            return _isScalar[(int)reader.Kind];
        }
    }
}
