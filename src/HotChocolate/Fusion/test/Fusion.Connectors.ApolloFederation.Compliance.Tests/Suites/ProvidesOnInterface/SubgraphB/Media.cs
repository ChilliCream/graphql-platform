namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// Marker interface for the <c>Media</c> type hierarchy in the
/// <c>subgraph-b</c> subgraph.
/// </summary>
public interface IMedia
{
    string Id { get; }

    List<IAnimal>? Animals { get; }
}
