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

        public static Token ExpectAt(this IParserContext context)
        {
            return Expect(context, t => t.IsAt());
        }


        public static Token ExpectRightBracket(this IParserContext context)
        {
            return Expect(context, t => t.IsAt());
        }

        public static Token Expect(this IParserContext context, Func<Token, bool> expectation)
        {
            if (expectation(context.Token))
            {
                return context.MoveNext().Previous;
            }

            throw new SyntaxException(context,
                $"Expected a name token: {context.Token}.");
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

        public static Token ExpectKeyword(IParserContext context, string keyword)
        {
            Token token = context.Token;
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
            return null;
        }

        public static SyntaxException Unexpected(this IParserContext context, Token token)
        {
            return new SyntaxException(context, token,
                $"Unexpected token: {token}.");
        }

        public static Token SkipDescription(this IParserContext context)
        {
            if (context.Token.IsDescription())
            {
                return context.MoveNext();
            }
            return context.Token;
        }

        public static bool Skip(this IParserContext context, TokenKind kind)
        {
            bool match = context.Token.Kind == kind;
            if (match)
            {
                context.MoveNext();
            }
            return match;
        }

        public static bool Peek(this IParserContext context, TokenKind kind)
        {
            return context.Peek().Kind != kind;
        }

        public static bool Peek(this IParserContext context, Func<Token, bool> condition)
        {
            return condition(context.Peek());
        }
    }


}