namespace Prometheus.Language
{
    public interface ILexer
    {
        Token Read(ISource source);
    }
}