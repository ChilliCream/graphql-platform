using System.Buffers;
using System;
using System.Runtime.CompilerServices;
using System.Globalization;
using HotChocolate.Language.Properties;

namespace HotChocolate.Language
{
    public ref partial struct TextGraphQLReader
    {
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
        internal ReadOnlySpan<char> Expect(TokenKind kind)
        {
            ReadOnlySpan<char> value = Value;

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
