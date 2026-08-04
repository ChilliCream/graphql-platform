namespace HotChocolate.Fusion.Suites.Typename.B;

/// <summary>
/// The <c>User</c> entity as projected by the <c>b</c> subgraph
/// (<c>type User @key(fields: "id") @interfaceObject { id, name }</c>).
/// The audit's <c>@interfaceObject</c> directive tells the gateway to
/// route <c>name</c> requests on every concrete implementer of the
/// <c>User</c> interface in subgraph <c>a</c> through this subgraph.
/// </summary>
public sealed class User
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
