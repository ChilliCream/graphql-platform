namespace Prometheus.Language
{
    public interface IExecutableDefinitionNode
        : IDefinitionNode
    {
        NameNode Name { get; }
    }
}