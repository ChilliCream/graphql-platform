using System;

namespace Prometheus.Language
{
    /// <summary>
    /// Reads comment tokens as specified in 
    /// http://facebook.github.io/graphql/October2016/#sec-Comments.
    /// #[\u0009\u0020-\uFFFF]*
    /// </summary>
    public class CommentTokenReader
        : TokenReaderBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.CommentTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
        public CommentTokenReader(ReadNextToken readNextTokenDelegate)
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
            return context.PeekTest(c => c.IsHash());
        }

        /// <summary>
        /// Reads a comment token from the lexer context.
        /// </summary>  
        /// <returns>
        /// Returns the comment token read from the lexer context.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
        public override Token ReadToken(ILexerContext context, Token previous)
        {
            int start = context.Position;
            context.Skip();

            while (context.PeekTest(c => !c.IsControlCharacter() || c.IsTab()))
            {
                context.Skip();
            }

            string comment = context.Read(start + 1, context.Position);
            return CreateToken(context, previous, TokenKind.Comment,
                start, comment.TrimStart(' ', '\t'));
        }
    }
}
