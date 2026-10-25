namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>a</c> subgraph
/// (<c>type Product @key(fields: "id") { id: ID @external, category: Category @shareable }</c>).
/// Subgraph <c>a</c> only owns the shareable <c>category</c> field;
/// <c>id</c> is external. The reference resolver builds the category
/// payload using the product id, so the value flows out of the
/// <c>__resolveReference</c> entity call.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public Category? Category { get; init; }
}
