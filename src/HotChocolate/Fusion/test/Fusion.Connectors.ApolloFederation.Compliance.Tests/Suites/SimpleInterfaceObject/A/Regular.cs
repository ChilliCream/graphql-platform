namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// The <c>Regular</c> concrete implementer of the <c>Account</c> interface in
/// the <c>a</c> subgraph
/// (<c>type Regular implements Account @key(fields: "id") { id, isMain }</c>).
/// </summary>
public sealed class Regular : IAccount
{
    public string Id { get; init; } = default!;

    public bool IsMain { get; init; }
}
