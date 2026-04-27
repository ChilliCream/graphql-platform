namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Marker interface for the <c>Animal</c> type hierarchy in the
/// <c>subgraph-b</c> subgraph.
/// </summary>
public interface IAnimal
{
    string Id { get; }

    string? Name { get; }
}
