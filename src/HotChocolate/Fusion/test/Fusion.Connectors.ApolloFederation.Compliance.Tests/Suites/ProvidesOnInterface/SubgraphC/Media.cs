namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// Marker interface for the <c>Media</c> type hierarchy in the
/// <c>subgraph-c</c> subgraph.
/// </summary>
public interface IMedia
{
    string Id { get; }

    List<IAnimal>? Animals { get; }
}
