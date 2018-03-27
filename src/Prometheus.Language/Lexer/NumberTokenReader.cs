namespace Prometheus.Language
{

    /// <summary>
	/// Reads int tokens as specified in 
	/// http://facebook.github.io/graphql/October2016/#IntValue
	/// or a float tokens as specified in
	/// http://facebook.github.io/graphql/October2016/#FloatValue.
    /// </summary>
	public class NumberTokenReader
		: TokenReaderBase
	{
		/// <summary>
        /// Initializes a new instance of the <see cref="T:Prometheus.Language.NumberTokenReader"/> class.
        /// </summary>
        /// <param name="readNextTokenDelegate">Read next token delegate.</param>
		public NumberTokenReader(ReadNextToken readNextTokenDelegate)
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
			return context.PeekTest(c => c.IsDigit() || c.IsMinus());
		}

		/// <summary>
        /// Reads an int or float token from the lexer context.
        /// </summary>  
        /// <returns>
		/// Returns the int or float token read from the lexer context.
        /// </returns>
        /// <param name="context">The lexer context.</param>
        /// <param name="previous">The previous-token.</param>
		public override Token ReadToken(ILexerContext context, Token previous)
		{
			int start = context.Position;
			char code = context.Read();
			bool isFloat = false;

			if (code.IsMinus())
			{
				code = context.Read();
			}

			if (code == '0')
			{
				code = context.Read();
				if (char.IsDigit(code))
				{
					throw new SyntaxException(context,
						$"Invalid number, unexpected digit after 0: {code}.");
				}
			}
			else
			{
				ReadDigits(context, code);
			}

			if (context.PeekTest(c => c.IsDot()))
			{
				isFloat = true;
				ReadDigits(context, context.Skip().Read());
			}

			if (context.PeekTest(c => c == 'E' || c == 'e'))
			{
				isFloat = true;

				code = context.Skip().Read();
				if (code.IsPlus() || code.IsMinus())
				{
					code = context.Read();
				}
				ReadDigits(context, code);
			}

			TokenKind kind = isFloat ? TokenKind.Float : TokenKind.Integer;
			return CreateToken(context, previous, kind, start,
				context.Read(start, context.Position));
		}

		private void ReadDigits(ILexerContext context, char firstCode)
		{
			if (!firstCode.IsDigit())
			{
				throw new SyntaxException(context,
					$"Invalid number, expected digit but got: {firstCode}.");
			}

			char code = firstCode;
			while (context.PeekTest(c => c.IsDigit()))
			{
				code = context.Read();
			}
		}
	}
}