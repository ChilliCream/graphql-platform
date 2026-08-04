namespace HotChocolate.Fusion.Suites.ParentEntityCall.C;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>c</c> subgraph
/// (<c>type Product @key(fields: "id pid") { id, pid, category }</c>). The
/// shareable <c>category</c> field flowing out of the parent entity call
/// is the only path through which <see cref="Category"/> is reachable in
/// this subgraph.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Pid { get; init; } = default!;

    public Category? Category { get; init; }
}
