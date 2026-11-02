namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// The <c>User</c> interface as projected by the <c>a</c> subgraph
/// (<c>interface User @key(fields: "id") { id: ID! }</c>). The audit's
/// subgraph <c>b</c> extends this same federated interface via
/// <c>@interfaceObject</c>.
/// </summary>
public interface IUser
{
    string Id { get; }
}
