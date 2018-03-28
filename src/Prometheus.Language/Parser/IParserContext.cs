namespace Prometheus.Language
{
    public interface IParserContext
    {
        ISource Source { get; }

        Token Token { get; }

        Token MoveNext();

        Token Peek();
    }


}