namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

/// <summary>
/// The <c>Account</c> interface object as projected by the <c>c</c> subgraph
/// (<c>type Account @key(fields: "id") @interfaceObject { id, isActive @shareable }</c>).
/// The <c>@interfaceObject</c> directive contributes <c>isActive</c> to every
/// concrete implementer of the <c>Account</c> interface owned by subgraph
/// <c>a</c>.
/// </summary>
public sealed class Account
{
    public string Id { get; init; } = default!;

    public bool IsActive { get; init; }
}
