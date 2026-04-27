namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;

/// <summary>
/// The <c>Dog</c> entity as projected by the <c>subgraph-a</c> subgraph
/// (<c>@key(fields: "id")</c>). Only the key field is known here;
/// <c>name</c> and <c>age</c> are owned by subgraph-c.
/// </summary>
public sealed class Dog : IAnimal
{
    public string Id { get; init; } = default!;
}
