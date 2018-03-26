namespace Prometheus.Language
{
    public interface IExecutableDefinitionNode
        : ISyntaxNode
    {
        NameNode Name { get; }
    }
}