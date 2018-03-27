using System;

namespace Prometheus.Language
{
	/// <summary>
	/// Reads name tokens as specified in 
	/// http://facebook.github.io/graphql/October2016/#Name
	/// [_A-Za-z][_0-9A-Za-z]
    /// </summary>
	public class NameTokenReader
		: TokenReaderBase
	{
		/// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.NameTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
		public NameTokenReader(ReadNextToken readNextTokenDelegate)
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
			return context.PeekTest(c => c.IsLetter() || c.IsUnderscore());
		}

		/// <summary>
        /// Reads a name token from the lexer context.
        /// </summary>  
        /// <returns>
        /// Returns the name token read from the lexer context.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
		public override Token ReadToken(ILexerContext context, Token previous)
		{
			int start = context.Position;

			while (context.PeekTest(c => c.IsLetterOrDigit() || c.IsUnderscore()))
			{
				context.Read();
			}

			return CreateToken(context, previous, TokenKind.Name,
				start, context.Read(start, context.Position));
		}
	}
}