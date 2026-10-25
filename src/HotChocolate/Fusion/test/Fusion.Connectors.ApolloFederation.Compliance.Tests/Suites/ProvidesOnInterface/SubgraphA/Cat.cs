namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// The <c>Cat</c> entity as projected by the <c>subgraph-a</c> subgraph
/// (<c>@key(fields: "id")</c>). The <c>id</c> field is external.
/// </summary>
public sealed class Cat : IAnimal
{
    public string Id { get; init; } = default!;
}
