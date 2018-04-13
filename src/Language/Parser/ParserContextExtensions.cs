using System;

namespace HotChocolate.Language
{
    internal static class ParserContextExtensions
    {
        public static Token ExpectName(this ParserContext context)
        {
            return Expect(context, TokenKind.Name);
        }

        public static Token ExpectColon(this ParserContext context)
        {
            return Expect(context, TokenKind.Colon);
        }

        public static Token ExpectDollar(this ParserContext context)
        {
            return Expect(context, TokenKind.Dollar);
        }

        public static Token ExpectAt(this ParserContext context)
        {
            return Expect(context, TokenKind.At);
        }

        public static Token ExpectRightBracket(this ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static Token ExpectLeftBrace(this ParserContext context)
        {
            return Expect(context, TokenKind.RightBracket);
        }

        public static Token ExpectString(this ParserContext context)
        {
            if (context.Current.IsString())
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static Token ExpectScalarValue(this ParserContext context)
        {
             if (context.Current.IsScalarValue())
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static Token ExpectSpread(this ParserContext context)
        {
            return Expect(context, TokenKind.Spread);
        }

        public static Token Expect(this ParserContext context, TokenKind kind)
        {
            if (context.Current.Kind == kind)
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static Token ExpectScalarKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Scalar);
        }

        public static Token ExpectSchemaKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Schema);
        }

        public static Token ExpectTypeKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Type);
        }

        public static Token ExpectInterfaceKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Interface);
        }

        public static Token ExpectUnionKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Union);
        }

        public static Token ExpectEnumKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Enum);
        }

        public static Token ExpectInputKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Input);
        }

        public static Token ExpectExtendKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Extend);
        }

        public static Token ExpectDirectiveKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Directive);
        }

        public static Token ExpectOnKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.On);
        }

        public static Token ExpectFragmentKeyword(this ParserContext context)
        {
            return ExpectKeyword(context, Keywords.Fragment);
        }

        public static Token ExpectKeyword(ParserContext context, string keyword)
        {
            Token token = context.Current;
            if (token.IsName() && token.Value == keyword)
            {
                context.MoveNext();
                return token;
            }
            throw new SyntaxException(context,
                $"Expected \"{keyword}\", found {token}");
        }

        public static Location CreateLocation(this ParserContext context, Token start)
        {
            if (context.Options.NoLocations)
            {
                return null;
            }
            return new Location(context.Source, start, context.Current);
        }

        public static SyntaxException Unexpected(this ParserContext context, Token token)
        {
            return new SyntaxException(context, token,
                $"Unexpected token: {token}.");
        }

        public static Token SkipDescription(this ParserContext context)
        {
            if (context.Current.IsDescription())
            {
                context.MoveNext();
                return context.Current;
            }
            return context.Current;
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

        public static bool SkipKeyword(this ParserContext context, string keyword)
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