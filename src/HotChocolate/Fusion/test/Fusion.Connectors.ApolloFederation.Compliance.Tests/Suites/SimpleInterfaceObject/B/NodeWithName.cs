namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// The <c>NodeWithName</c> interface object as projected by the <c>b</c>
/// subgraph
/// (<c>type NodeWithName @key(fields: "id") @interfaceObject { id, username }</c>).
/// The <c>@interfaceObject</c> directive contributes <c>username</c> to every
/// concrete implementer of the <c>NodeWithName</c> interface owned by
/// subgraph <c>a</c>.
/// </summary>
public sealed class NodeWithName
{
    public string Id { get; init; } = default!;

    public string? Username { get; init; }
}
