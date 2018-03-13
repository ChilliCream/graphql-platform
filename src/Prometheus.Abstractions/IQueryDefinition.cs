namespace Prometheus.Abstractions
{
    public interface IQueryDefinition
        : IHasSelectionSet
    {
        string Name { get; }
    }
}