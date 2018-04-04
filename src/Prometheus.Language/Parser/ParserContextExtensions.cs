using System;

namespace Prometheus.Language
{
    public static class ParserContextExtensions
    {
        public static Token ExpectName(this IParserContext context)
        {
            return Expect(context, t => t.IsName());
        }

        public static Token ExpectColon(this IParserContext context)
        {
            return Expect(context, t => t.IsColon());
        }

        public static Token ExpectDollar(this IParserContext context)
        {
            return Expect(context, t => t.IsDollar());
        }

        public static Token ExpectAt(this IParserContext context)
        {
            return Expect(context, t => t.IsAt());
        }

        public static Token ExpectRightBracket(this IParserContext context)
        {
            return Expect(context, t => t.IsRightBracket());
        }

        public static Token ExpectLeftBrace(this IParserContext context)
        {
            return Expect(context, t => t.IsLeftBrace());
        }

        public static Token ExpectString(this IParserContext context)
        {
            return Expect(context, t => t.IsString());
        }

        public static Token ExpectScalarValue(this IParserContext context)
        {
            return Expect(context, t => t.IsScalarValue());
        }

        public static Token Expect(this IParserContext context, Func<Token, bool> expectation)
        {
            if (expectation(context.Current))
            {
                context.MoveNext();
                return context.Current.Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Current}.");
        }

        public static Token ExpectScalarKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Scalar);
        }

        public static Token ExpectSchemaKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Schema);
        }

        public static Token ExpectTypeKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Type);
        }

        public static Token ExpectInterfaceKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Interface);
        }

        public static Token ExpectUnionKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Union);
        }

        public static Token ExpectEnumKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Enum);
        }

        public static Token ExpectInputKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Input);
        }

        public static Token ExpectExtendKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Extend);
        }

        public static Token ExpectDirectiveKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.Directive);
        }

        public static Token ExpectOnKeyword(this IParserContext context)
        {
            return ExpectKeyword(context, Keywords.On);
        }

        public static Token ExpectKeyword(IParserContext context, string keyword)
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

        public static Location CreateLocation(this IParserContext context, Token start)
        {
            return new Location(context.Source, start, context.Current);
        }

        public static SyntaxException Unexpected(this IParserContext context, Token token)
        {
            return new SyntaxException(context, token,
                $"Unexpected token: {token}.");
        }

        public static Token SkipDescription(this IParserContext context)
        {
            if (context.Current.IsDescription())
            {
                context.MoveNext();
                return context.Current;
            }
            return context.Current;
        }

        public static bool Skip(this IParserContext context, TokenKind kind)
        {
            bool match = context.Current.Kind == kind;
            if (match)
            {
                context.MoveNext();
            }
            return match;
        }

        public static bool SkipKeyword(this IParserContext context, string keyword)
        {
            if (context.Current.IsName() && context.Current.Value == keyword)
            {
                context.MoveNext();
                return true;
            }
            return false;
        }

        public static bool Peek(this IParserContext context, TokenKind kind)
        {
            return context.Peek().Kind == kind;
        }

        public static bool Peek(this IParserContext context, Func<Token, bool> condition)
        {
            return condition(context.Peek());
        }

        public static bool IsEndOfFile(this IParserContext context)
        {
            return context.Current.Kind == TokenKind.EndOfFile;
        }
    }
}