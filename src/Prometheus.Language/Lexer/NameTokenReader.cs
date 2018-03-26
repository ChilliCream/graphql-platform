namespace Prometheus.Language
{
	/// <summary>
	/// Reads name tokens specified in 
	/// http://facebook.github.io/graphql/October2016/#Name
	/// [_A-Za-z][_0-9A-Za-z]ßßß
    /// </summary>
	public class NameTokenReader
		: TokenReaderBase
	{
		public NameTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(c => c.IsLetter() || c.IsUnderscore());
		}

	
        /// <summary>
		/// Reads an alphanumeric + underscore name token from the <see cref="ISource"/>
        /// </summary>
		/// <returns>
        /// Returns the punctuator token read from the source stream.
        /// </returns>
        /// <param name="context">Context.</param>
        /// <param name="previous">Previous.</param>
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