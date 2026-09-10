namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// The <c>Admin</c> concrete implementer of the <c>User</c> interface in
/// the <c>a</c> subgraph
/// (<c>type Admin implements User @key(fields: "id") { id: ID!, isMain: Boolean! }</c>).
/// </summary>
public sealed class Admin : IUser
{
    public string Id { get; init; } = default!;

    public bool IsMain { get; init; }
}
