namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;

/// <summary>
/// The <c>User</c> entity as projected by the <c>a</c> subgraph. Implements the
/// federated <c>NodeWithName</c> interface (<c>@key(fields: "id")</c>) and owns
/// the <c>name</c> and <c>age</c> fields.
/// </summary>
public sealed class User : INodeWithName
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public int? Age { get; init; }
}
