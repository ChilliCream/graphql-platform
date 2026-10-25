namespace HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

/// <summary>
/// The <c>@interfaceObject</c> stand-in for the federated <c>NodeWithName</c>
/// interface in the <c>b</c> subgraph. The <c>name</c> field is external and is
/// required to resolve <c>username</c>.
/// </summary>
public sealed class NodeWithName
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
