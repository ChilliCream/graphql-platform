namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// The <c>Account</c> interface as projected by the <c>a</c> subgraph
/// (<c>interface Account @key(fields: "id") { id: ID! }</c>). The audit's
/// subgraphs <c>b</c> and <c>c</c> extend this same federated interface via
/// <c>@interfaceObject</c>.
/// </summary>
public interface IAccount
{
    string Id { get; }
}
