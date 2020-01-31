using System.Buffers;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        public string GetString()
        {
            if (_value.Length == 0)
            {
                return string.Empty;
            }

            return GetString(_value, _kind == TokenKind.BlockString);
        }

        public static string GetString(
            ReadOnlySpan<byte> escapedValue,
            bool isBlockString)
        {
            if (escapedValue.Length == 0)
            {
                return string.Empty;
            }

            int length = checked(escapedValue.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[]? unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                UnescapeValue(escapedValue, ref unescapedSpan, isBlockString);
                return GetString(unescapedSpan);
            }
            finally
            {
                if (unescapedArray != null)
                {
                    unescapedSpan.Clear();
                    ArrayPool<byte>.Shared.Return(unescapedArray);
                }
            }
        }

        internal static string GetScalarValue(ReadOnlySpan<byte> unescapedValue) =>
            GetString(unescapedValue);

        public unsafe static string GetString(ReadOnlySpan<byte> unescapedValue)
        {
            if (unescapedValue.Length == 0)
            {
                return string.Empty;
            }

            fixed (byte* bytePtr = unescapedValue)
            {
                return StringHelper.UTF8Encoding
                    .GetString(bytePtr, unescapedValue.Length);
            }
        }

        public string GetComment()
        {
            if (_value.Length != 0)
            {
                StringHelper.TrimStringToken(ref _value);
            }

            return GetString(_value);
        }

        public string GetName() => GetString(_value);
        public string GetScalarValue() => GetString(_value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UnescapeValue(
            in ReadOnlySpan<byte> escaped,
            ref Span<byte> unescapedValue,
            bool isBlockString)
        {
            Utf8Helper.Unescape(
                in escaped,
                ref unescapedValue,
                isBlockString);

            if (isBlockString)
            {
                StringHelper.TrimBlockStringToken(
                    unescapedValue, ref unescapedValue);
            }
        }

        public void UnescapeValue(ref Span<byte> unescapedValue)
        {
            if (_value.Length == 0)
            {
                unescapedValue = unescapedValue.Slice(0, 0);
            }
            else
            {
                UnescapeValue(
                    in _value,
                    ref unescapedValue,
                    _kind == TokenKind.BlockString);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext()
        {
            while (Read() && _kind == TokenKind.Comment)
            { }
            return !IsEndOfStream();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Skip(TokenKind kind)
        {
            if (_kind == kind)
            {
                MoveNext();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool SkipKeyword(ReadOnlySpan<byte> keyword)
        {
            if (Kind == TokenKind.Name && Value.SequenceEqual(keyword))
            {
                MoveNext();
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<byte> Expect(TokenKind kind)
        {
            ReadOnlySpan<byte> value = Value;

            if (!Skip(kind))
            {
                throw new SyntaxException(this,
                    string.Format(CultureInfo.InvariantCulture,
                        LangResources.Parser_InvalidToken,
                        kind,
                        Kind));
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExpectKeyword(ReadOnlySpan<byte> keyword)
        {
            if (!SkipKeyword(keyword))
            {
                string found = Kind == TokenKind.Name
                    ? GetName()
                    : Kind.ToString();

                throw new SyntaxException(this,
                    string.Format(CultureInfo.InvariantCulture,
                        LangResources.Parser_InvalidToken,
                        Utf8GraphQLReader.GetString(keyword),
                        found));
            }
        }
    }
}
