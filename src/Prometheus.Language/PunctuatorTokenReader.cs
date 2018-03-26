namespace Prometheus.Language
{
	/// <summary>
	/// Reads punctuator token specified in 
	/// http://facebook.github.io/graphql/October2016/#sec-Language
	/// section 2.1.8.
	/// </summary>
	public class PunctuatorTokenReader
		: TokenReaderBase
	{
		public PunctuatorTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{

		}

		/// <summary>
		/// Reads a punctuator token.
		/// </summary>	
		/// <returns>
		/// Returns the punctuator token read from the source stream.
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
			return CreateToken(context, previous, kind, context.Position);
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