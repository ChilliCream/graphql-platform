using System;

namespace HotChocolate.Language
{
    internal static class ParserHelper
    {
        public static string ExpectName(in Utf8GraphQLReader reader)
        {
            if (reader.Kind == TokenKind.Name)
            {
                string name = reader.GetString(reader.Value);
                reader.Read();
                return name;
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {TokenVisualizer.Visualize(reader)}.");
        }

        public static void ExpectColon(in Utf8GraphQLReader reader)
        {
            Expect(in reader, TokenKind.Colon);
        }

        public static void ExpectDollar(ParserContext context)
        {
            return Expect(context, TokenKind.Dollar);
        }

        public static void ExpectAt(ParserContext context)
        {
            return Expect(context, TokenKind.At);
        }

        public static void ExpectRightBracket(ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static void ExpectLeftBrace(ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static string ExpectString(in Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsString(in reader))
            {
                string value = reader.GetString();
                reader.Read();
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a string token: " +
                $"{TokenVisualizer.Visualize(reader)}.");
        }

        public static string ExpectScalarValue(in Utf8GraphQLReader reader)
        {
            if (TokenHelper.IsScalarValue(in reader))
            {
                string value = reader.GetString(reader.Value);
                reader.Read();
                return value;
            }

            throw new SyntaxException(reader,
                "Expected a scalar value token: " +
                $"{TokenVisualizer.Visualize(reader)}.");
        }


        public static SyntaxToken ExpectSpread(ParserContext context)
        {
            return Expect(context, TokenKind.Spread);
        }

        public static void Expect(
            in Utf8GraphQLReader reader,
            TokenKind kind)
        {
            if (reader.Kind == kind)
            {
                reader.Read();
            }

            throw new SyntaxException(reader,
                $"Expected a name token: {reader.Kind}.");
        }

        public static SyntaxToken ExpectScalarKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Scalar);
        }

        public static SyntaxToken ExpectSchemaKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Schema);
        }

        public static SyntaxToken ExpectTypeKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Type);
        }

        public static SyntaxToken ExpectInterfaceKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Interface);
        }

        public static SyntaxToken ExpectUnionKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Union);
        }

        public static SyntaxToken ExpectEnumKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Enum);
        }

        public static SyntaxToken ExpectInputKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Input);
        }

        public static SyntaxToken ExpectExtendKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Extend);
        }

        public static SyntaxToken ExpectDirectiveKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Utf8Keywords.Directive);
        }

        public static SyntaxToken ExpectOnKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.On);
        }

        public static SyntaxToken ExpectFragmentKeyword(
            this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Fragment);
        }

        public static void ExpectKeyword(
            in Utf8GraphQLReader reader,
            byte[] keyword)
        {
            if (TokenHelper.IsName(in reader)
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
            in Utf8GraphQLReader reader)
        {
            return new SyntaxTokenInfo(
                reader.Kind,
                reader.Start,
                reader.End,
                reader.Line,
                reader.Column);
        }

        public static SyntaxException Unexpected(
            in Utf8GraphQLReader reader, TokenKind kind)
        {
            return new SyntaxException(reader,
                $"Unexpected token: {TokenVisualizer.Visualize(kind)}.");
        }

        public static SyntaxToken SkipDescription(this ParserContext context)
        {
            if (context.Current.IsDescription())
            {
                context.MoveNext();
                return context.Current;
            }
            return context.Current;
        }

        public static void SkipWhile(this ParserContext context, TokenKind kind)
        {
            while (context.Skip(kind)) ;
        }

        public static bool Skip(this ParserContext context, TokenKind kind)
        {
            bool match = context.Current.Kind == kind;
            if (match)
            {
                context.MoveNext();
            }
            return match;
        }

        public static bool SkipRepeatableKeyword(this ParserContext context)
        {
            return SkipKeyword(context, Keywords.Repeatable);
        }

        public static bool SkipKeyword(
            this ParserContext context,
            string keyword)
        {
            if (context.Current.IsName() && context.Current.Value == keyword)
            {
                context.MoveNext();
                return true;
            }
            return false;
        }

        public static bool Peek(this ParserContext context, TokenKind kind)
        {
            return context.Current.Peek().Kind == kind;
        }

        public static bool IsEndOfFile(this ParserContext context)
        {
            return context.Current.Kind == TokenKind.EndOfFile;
        }
    }
}
