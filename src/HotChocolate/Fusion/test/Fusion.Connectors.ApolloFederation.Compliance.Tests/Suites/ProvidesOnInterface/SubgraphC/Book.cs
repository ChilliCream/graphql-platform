namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// The <c>Book</c> entity as projected by the <c>subgraph-c</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>animals</c> (shareable).
/// </summary>
public sealed class Book : IMedia
{
    public string Id { get; init; } = default!;

    public List<IAnimal>? Animals { get; init; }
}
