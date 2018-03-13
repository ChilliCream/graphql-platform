namespace Prometheus.Abstractions
{
    public interface IHasSelectionSet
    {
        ISelectionSet SelectionSet { get; }
    }
}