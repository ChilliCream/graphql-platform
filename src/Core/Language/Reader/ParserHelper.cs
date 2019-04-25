using System;
using System.Runtime.CompilerServices;

namespace HotChocolate.Language
{
    internal static class ParserHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ExpectName(ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.Name)
            {
                string name = reader.GetString(reader.Value);
                MoveNext(ref reader);
                return name;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {TokenVisualizer.Visualize(in reader)}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectColon(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Colon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectDollar(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Dollar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectAt(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.At);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectRightBracket(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.RightBracket);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ExpectString(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsString(ref reader))
            {
                string value = reader.GetString();
                MoveNext(ref reader);
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a string token: " +
                $"{TokenVisualizer.Visualize(in reader)}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ExpectScalarValue(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsScalarValue(ref reader))
            {
                string value = reader.GetString(reader.Value);
                MoveNext(ref reader);
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a scalar value token: " +
                $"{TokenVisualizer.Visualize(in reader)}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectSpread(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Spread);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Expect(
            ref Utf8GraphQLReader reader,
            TokenKind kind)
        {
            if (reader.Kind == kind)
            {
                MoveNext(ref reader);
                return;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {reader.Kind}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectSchemaKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Schema);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectDirectiveKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Directive);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectOnKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.On);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectFragmentKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Fragment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExpectKeyword(
            ref Utf8GraphQLReader reader,
            byte[] keyword)
        {
            if (TokenHelper.IsName(ref reader)
                && reader.Value.SequenceEqual(keyword))
            {
                MoveNext(ref reader);
                return;
            }
            throw new SyntaxException(reader,
                $"Expected \"{keyword}\", found " +
                $"{TokenVisualizer.Visualize(in reader)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SyntaxTokenInfo CreateTokenInfo(
            ref Utf8GraphQLReader reader)
        {
            return new SyntaxTokenInfo(
                reader.Kind,
                reader.Start,
                reader.End,
                reader.Line,
                reader.Column);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SyntaxException Unexpected(
            ref Utf8GraphQLReader reader, TokenKind kind)
        {
            return new SyntaxException(reader,
                $"Unexpected token: {TokenVisualizer.Visualize(kind)}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipDescription(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsDescription(ref reader))
            {
                MoveNext(ref reader);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Skip(ref Utf8GraphQLReader reader, TokenKind kind)
        {
            if (reader.Kind == kind)
            {
                return MoveNext(ref reader);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipRepeatableKeyword(ref Utf8GraphQLReader reader)
        {
            return SkipKeyword(ref reader, Utf8Keywords.Repeatable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SkipKeyword(
            ref Utf8GraphQLReader reader,
            byte[] keyword)
        {
            if (TokenHelper.IsName(ref reader)
                && reader.Value.SequenceEqual(keyword))
            {
                MoveNext(ref reader);
                return true;
            }
            return false;
        }

        public static bool MoveNext(ref Utf8GraphQLReader reader)
        {
            while (reader.Read() && reader.Kind == TokenKind.Comment) ;
            return !reader.IsEndOfStream();
        }
    }
}
