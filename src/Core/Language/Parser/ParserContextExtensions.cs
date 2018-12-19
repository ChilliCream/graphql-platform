namespace HotChocolate.Language
{
    internal static class ParserContextExtensions
    {
        public static SyntaxToken ExpectName(this ParserContext context)
        {
            return Expect(context, TokenKind.Name);
        }

        public static SyntaxToken ExpectColon(this ParserContext context)
        {
            return Expect(context, TokenKind.Colon);
        }

        public static SyntaxToken ExpectDollar(this ParserContext context)
        {
            return Expect(context, TokenKind.Dollar);
        }

        public static SyntaxToken ExpectAt(this ParserContext context)
        {
            return Expect(context, TokenKind.At);
        }

        public static SyntaxToken ExpectRightBracket(this ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static SyntaxToken ExpectLeftBrace(this ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static SyntaxToken ExpectString(this ParserContext context)
        {
            if (context.Current.IsString())
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static SyntaxToken ExpectScalarValue(this ParserContext context)
        {
            if (context.Current.IsScalarValue())
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static SyntaxToken ExpectSpread(this ParserContext context)
        {
            return Expect(context, TokenKind.Spread);
        }

        public static SyntaxToken Expect(
            this ParserContext context,
            TokenKind kind)
        {
            SyntaxToken current = context.Current;
            if (current.Kind == kind)
            {
                context.MoveNext();
                return current;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
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
            return ExpectKeyword(context, Keywords.Directive);
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

        public static SyntaxToken ExpectKeyword(
            ParserContext context,
            string keyword)
        {
            SyntaxToken token = context.Current;
            if (token.IsName() && token.Value == keyword)
            {
                context.MoveNext();
                return token;
            }
            throw new SyntaxException(context,
                $"Expected \"{keyword}\", found {token}");
        }

        public static Location CreateLocation(
            this ParserContext context,
            SyntaxToken start)
        {
            if (context.Options.NoLocations)
            {
                return null;
            }
            return new Location(context.Source, start, context.Current);
        }

        public static SyntaxException Unexpected(
            this ParserContext context,
            SyntaxToken token)
        {
            return new SyntaxException(context, token,
                $"Unexpected token: {token}.");
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
