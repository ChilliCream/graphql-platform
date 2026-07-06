namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// The <c>Account</c> interface object as projected by the <c>b</c> subgraph
/// (<c>type Account @key(fields: "id") @interfaceObject { id, name }</c>).
/// The <c>@interfaceObject</c> directive contributes <c>name</c> to every
/// concrete implementer of the <c>Account</c> interface owned by subgraph
/// <c>a</c>.
/// </summary>
public sealed class Account
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
