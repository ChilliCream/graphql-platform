namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// Marker interface for the <c>Animal</c> type hierarchy in the
/// <c>subgraph-a</c> subgraph. Only declares <c>id</c> here;
/// <c>name</c> is owned by subgraph-c.
/// </summary>
public interface IAnimal
{
    string Id { get; }
}
