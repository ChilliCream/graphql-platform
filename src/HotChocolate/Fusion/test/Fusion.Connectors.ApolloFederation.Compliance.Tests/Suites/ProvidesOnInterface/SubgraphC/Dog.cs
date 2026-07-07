namespace HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

/// <summary>
/// The <c>Dog</c> entity as projected by the <c>subgraph-c</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>name</c> (shareable) and <c>age</c>.
/// </summary>
public sealed class Dog : IAnimal
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public int? Age { get; init; }
}
