namespace Prometheus.Language
{
	public delegate Token ReadNextToken(ILexerContext context, Token previous);

	public interface ITokenReader
	{
		Token ReadToken(ILexerContext context, Token previous);
	}
}