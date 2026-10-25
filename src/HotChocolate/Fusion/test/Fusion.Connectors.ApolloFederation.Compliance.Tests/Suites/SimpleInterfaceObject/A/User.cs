namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// The <c>User</c> entity as projected by the <c>a</c> subgraph
/// (<c>type User implements NodeWithName @key(fields: "id") { id, name, age }</c>).
/// </summary>
public sealed class User : INodeWithName
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public int? Age { get; init; }
}
