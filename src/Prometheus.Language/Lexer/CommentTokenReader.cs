namespace Prometheus.Language
{   
    /// <summary>
	/// Reads comment tokens specified in 
	/// http://facebook.github.io/graphql/October2016/#sec-Comments
	/// #[\u0009\u0020-\uFFFF]*
    /// </summary>
	public class CommentTokenReader
		: TokenReaderBase
	{
		public CommentTokenReader(ReadNextToken readNextTokenDelegate)
			: base(readNextTokenDelegate)
		{
		}

		public override bool CanHandle(ILexerContext context)
		{
			return context.PeekTest(c => c.IsHash());
		}

		public override Token ReadToken(ILexerContext context, Token previous)
		{
			int start = context.Position;
			context.Skip();

			while (context.PeekTest(c => c > 0x001f || c.IsTab()))
			{
				context.Skip();
			}

			return CreateToken(context, previous, TokenKind.Comment,
				start, context.Read(start, context.Position));
		}
	}
}
