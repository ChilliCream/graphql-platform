using System.Buffers;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct Utf8GraphQLReader
    {
        public unsafe string GetString()
        {
            if (_value.Length == 0)
            {
                return string.Empty;
            }

            bool isBlockString = _kind == TokenKind.BlockString;

            int length = checked((int)_value.Length);
            bool useStackalloc =
                length <= GraphQLConstants.StackallocThreshold;

            byte[] unescapedArray = null;

            Span<byte> unescapedSpan = useStackalloc
                ? stackalloc byte[length]
                : (unescapedArray = ArrayPool<byte>.Shared.Rent(length));

            try
            {
                UnescapeValue(_value, ref unescapedSpan, isBlockString);

                fixed (byte* bytePtr = unescapedSpan)
                {
                    return StringHelper.UTF8Encoding.GetString(
                        bytePtr,
                        unescapedSpan.Length);
                }
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

        public unsafe string GetString(ReadOnlySpan<byte> unescapedValue)
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
            if (_value.Length > 0)
            {
                StringHelper.TrimStringToken(ref _value);
            }

            return GetString(_value);
        }

        public string GetName() => GetString(_value);
        public string GetScalarValue() => GetString(_value);

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
            while (Read() && _kind == TokenKind.Comment) { }
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
    }
}
