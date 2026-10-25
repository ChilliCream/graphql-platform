namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// The <c>Book</c> type in <c>subgraph-b</c>. Not keyed here;
/// <c>id</c> is shareable and <c>animals</c> is external.
/// </summary>
public sealed class Book : IMedia
{
    public string Id { get; init; } = default!;

    public List<IAnimal>? Animals { get; init; }
}
