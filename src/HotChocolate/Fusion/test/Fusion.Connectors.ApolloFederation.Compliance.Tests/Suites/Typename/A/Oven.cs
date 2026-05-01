namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// The <c>Oven</c> concrete type as projected by the <c>a</c> subgraph
/// (<c>type Oven implements Node { id: ID! }</c>). Member of the
/// <c>Product</c> union and the <c>Node</c> interface.
/// </summary>
public sealed class Oven : INode
{
    public string Id { get; init; } = default!;
}
