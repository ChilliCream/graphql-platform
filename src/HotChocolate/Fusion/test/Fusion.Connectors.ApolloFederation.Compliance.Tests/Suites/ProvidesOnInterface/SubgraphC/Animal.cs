namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Marker interface for the <c>Animal</c> type hierarchy in the
/// <c>subgraph-c</c> subgraph.
/// </summary>
public interface IAnimal
{
    string Id { get; }

    string? Name { get; }
}
