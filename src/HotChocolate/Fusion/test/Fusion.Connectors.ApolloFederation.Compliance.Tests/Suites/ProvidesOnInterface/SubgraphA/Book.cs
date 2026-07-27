namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// The <c>Book</c> entity as projected by the <c>subgraph-a</c> subgraph
/// (<c>@key(fields: "id")</c>). Implements both <c>Media</c> and owns
/// the <c>animals</c> relationship.
/// </summary>
public sealed class Book : IMedia
{
    public string Id { get; init; } = default!;

    public List<IAnimal>? Animals { get; init; }
}
