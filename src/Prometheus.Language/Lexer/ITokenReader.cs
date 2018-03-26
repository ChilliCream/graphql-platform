namespace Prometheus.Language
{
	public delegate Token ReadNextToken(ILexerContext context, Token previous);

	public interface ITokenReader
	{
		bool CanHandle(ILexerContext context);
		Token ReadToken(ILexerContext context, Token previous);
	}
}