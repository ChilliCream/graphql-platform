using System;

namespace Prometheus.Language
{
    /// <summary>
    /// Reads punctuator tokens as specified in 
    /// http://facebook.github.io/graphql/October2016/#sec-Punctuators
    /// one of ! $ ( ) ... : = @ [ ] { | }
    /// additionaly the reader will tokenize ampersands.
    /// </summary>
    internal class PunctuatorTokenReader
        : TokenReaderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.PunctuatorTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
        public PunctuatorTokenReader(ReadNextToken readNextTokenDelegate)
            : base(readNextTokenDelegate)
        {

        }

        /// <summary>
        /// Defines if this <see cref="ITokenReader"/> is able to 
        /// handle the next token.
        /// </summary>
        /// <returns>
        /// <c>true</c>, if this <see cref="ITokenReader"/> is able to 
        /// handle the next token, <c>false</c> otherwise.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="context"/> is <c>null</c>.
        /// </exception>
        public override bool CanHandle(ILexerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return context.PeekTest(c => c.IsPunctuator());
        }

        /// <summary>
        /// Reads a punctuator token from the lexer context.
        /// </summary>  
        /// <returns>
        /// Returns the punctuator token read from the lexer context.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
        public override Token ReadToken(ILexerContext context, Token previous)
        {
            if (context.PeekTest(c => c.IsDot(), c => c.IsDot(), c => c.IsDot()))
            {
                context.Skip(3);
                return CreateToken(context, previous, TokenKind.Spread, context.Position - 3);
            }

            TokenKind kind = LookupPunctuator(context, context.Read());
            return CreateToken(context, previous, kind, context.Position - 1);
        }

        /// <summary>
        /// Lookups the <see cref="TokenKind" /> for the current punctuator token.
        /// </summary>
        /// <returns>
        /// The <see cref="TokenKind" /> for the current punctuator token.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="code">The char representing the punctuator token.</param>
        /// <exception cref="SyntaxException">
        /// The code does not represent a valid punctiator token.
        /// </exception>
        private TokenKind LookupPunctuator(ILexerContext context, char code)
        {
            switch (code)
            {
                case '!': return TokenKind.Bang;
                case '$': return TokenKind.Dollar;
                case '&': return TokenKind.Ampersand;
                case '(': return TokenKind.LeftParenthesis;
                case ')': return TokenKind.RightParenthesis;
                case ':': return TokenKind.Colon;
                case '=': return TokenKind.Equal;
                case '@': return TokenKind.At;
                case '[': return TokenKind.LeftBracket;
                case ']': return TokenKind.RightBracket;
                case '{': return TokenKind.LeftBrace;
                case '|': return TokenKind.Pipe;
                case '}': return TokenKind.RightBrace;
                default:
                    throw new SyntaxException(context,
                        $"Invalid punctuator code: {code}.");
            }
        }
    }
}