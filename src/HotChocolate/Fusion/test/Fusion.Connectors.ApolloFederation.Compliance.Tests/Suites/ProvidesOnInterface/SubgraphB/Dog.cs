namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;

/// <summary>
/// The <c>Dog</c> type in <c>subgraph-b</c>. Not keyed;
/// <c>id</c> and <c>name</c> are external.
/// </summary>
public sealed class Dog : IAnimal
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
