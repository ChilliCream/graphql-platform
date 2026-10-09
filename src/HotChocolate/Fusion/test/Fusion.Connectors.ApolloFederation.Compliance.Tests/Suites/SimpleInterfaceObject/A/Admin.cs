namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// The <c>Admin</c> concrete implementer of the <c>Account</c> interface in
/// the <c>a</c> subgraph
/// (<c>type Admin implements Account @key(fields: "id") { id, isMain, isActive @shareable }</c>).
/// </summary>
public sealed class Admin : IAccount
{
    public string Id { get; init; } = default!;

    public bool IsMain { get; init; }

    public bool IsActive { get; init; }
}
