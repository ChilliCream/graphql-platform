namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;

/// <summary>
/// The <c>NodeWithName</c> interface as projected by the <c>a</c> subgraph
/// (<c>interface NodeWithName @key(fields: "id") { id: ID!, name: String }</c>).
/// The audit's subgraph <c>b</c> extends this same federated interface via
/// <c>@interfaceObject</c> to contribute the <c>username</c> field.
/// </summary>
public interface INodeWithName
{
    string Id { get; }

    string? Name { get; }
}
