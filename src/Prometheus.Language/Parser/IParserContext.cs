namespace Prometheus.Language
{
    public interface IParserContext
    {
        ISource Source { get; }

        Token Current { get; }

        bool MoveNext();

        Token Peek();
    }
}