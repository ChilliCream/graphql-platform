namespace HotChocolate.Fusion.Suites.Typename.A;

/// <summary>
/// The <c>Toaster</c> concrete type as projected by the <c>a</c> subgraph
/// (<c>type Toaster implements Node { id: ID! }</c>). Member of the
/// <c>Product</c> union and the <c>Node</c> interface.
/// </summary>
public sealed class Toaster : INode
{
    public string Id { get; init; } = default!;
}
