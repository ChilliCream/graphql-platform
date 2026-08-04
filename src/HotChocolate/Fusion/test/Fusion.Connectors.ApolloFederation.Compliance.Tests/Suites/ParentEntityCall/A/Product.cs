namespace HotChocolate.Fusion.Suites.ParentEntityCall.A;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>a</c> subgraph
/// (<c>type Product @key(fields: "id") @key(fields: "id pid") { id, pid, category }</c>).
/// Subgraph <c>a</c> owns the root <c>products</c> field and produces a
/// <see cref="Category"/> inline whose <c>id</c> is shareable.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Pid { get; init; } = default!;

    public Category? Category { get; init; }
}
