using System;

namespace HotChocolate.Language
{
    internal static class ParserHelper
    {
        public static string ExpectName(ref Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.Name)
            {
                string name = reader.GetString(reader.Value);
                reader.Read();
                return name;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {TokenVisualizer.Visualize(in reader)}.");
        }

        public static void ExpectColon(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Colon);
        }

        public static void ExpectDollar(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Dollar);
        }

        public static void ExpectAt(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.At);
        }

        public static void ExpectRightBracket(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.RightBracket);
        }

        public static void ExpectLeftBrace(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.RightBracket);
        }

        public static string ExpectString(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsString(ref reader))
            {
                string value = reader.GetString();
                reader.Read();
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a string token: " +
                $"{TokenVisualizer.Visualize(in reader)}.");
        }

        public static string ExpectScalarValue(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsScalarValue(ref reader))
            {
                string value = reader.GetString(reader.Value);
                reader.Read();
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a scalar value token: " +
                $"{TokenVisualizer.Visualize(in reader)}.");
        }


        public static void ExpectSpread(ref Utf8GraphQLReader reader)
        {
            Expect(ref reader, TokenKind.Spread);
        }

        public static void Expect(
            ref Utf8GraphQLReader reader,
            TokenKind kind)
        {
            if (reader.Kind == kind)
            {
                reader.Read();
                return;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {reader.Kind}.");
        }

        public static void ExpectScalarKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Scalar);
        }

        public static void ExpectSchemaKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Schema);
        }

        public static void ExpectTypeKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Type);
        }

        public static void ExpectInterfaceKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Interface);
        }

        public static void ExpectUnionKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Union);
        }

        public static void ExpectEnumKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Enum);
        }

        public static void ExpectInputKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Input);
        }

        public static void ExpectExtendKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Extend);
        }

        public static void ExpectDirectiveKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Directive);
        }

        public static void ExpectOnKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.On);
        }

        public static void ExpectFragmentKeyword(
            ref Utf8GraphQLReader reader)
        {
            ExpectKeyword(ref reader, Utf8Keywords.Fragment);
        }

        public static void ExpectKeyword(
            ref Utf8GraphQLReader reader,
            byte[] keyword)
        {
            if (TokenHelper.IsName(ref reader)
                && reader.Value.SequenceEqual(keyword))
            {
                reader.Read();
                return;
            }
            throw new SyntaxException(reader,
                $"Expected \"{keyword}\", found " +
                $"{TokenVisualizer.Visualize(in reader)}");
        }

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

        public static SyntaxException Unexpected(
            ref Utf8GraphQLReader reader, TokenKind kind)
        {
            return new SyntaxException(reader,
                $"Unexpected token: {TokenVisualizer.Visualize(kind)}.");
        }

        public static void SkipDescription(ref Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsDescription(ref reader))
            {
                reader.Read();
            }
        }

        public static void SkipWhile(ref Utf8GraphQLReader reader, TokenKind kind)
        {
            while (Skip(ref reader, kind)) ;
        }

        public static bool Skip(ref Utf8GraphQLReader reader, TokenKind kind)
        {
            if (reader.Kind == kind)
            {
                return reader.Read();
            }
            return false;
        }

        public static bool SkipRepeatableKeyword(ref Utf8GraphQLReader reader)
        {
            return SkipKeyword(ref reader, Utf8Keywords.Repeatable);
        }

        public static bool SkipKeyword(
            ref Utf8GraphQLReader reader,
            byte[] keyword)
        {
            if (TokenHelper.IsName(ref reader)
                && reader.Value.SequenceEqual(keyword))
            {
                reader.Read();
                return true;
            }
            return false;
        }
    }
}
