namespace Prometheus.Language
{
    internal interface ISyntaxNodeParser<out T>
        where T : ISyntaxNode
    {
        T Parse(IParserContext context);
    }
}