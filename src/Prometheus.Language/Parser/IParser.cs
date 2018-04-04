namespace Prometheus.Language
{
    public interface IParser
    {
        DocumentNode Parse(ILexer lexer, ISource source);
    }
}
