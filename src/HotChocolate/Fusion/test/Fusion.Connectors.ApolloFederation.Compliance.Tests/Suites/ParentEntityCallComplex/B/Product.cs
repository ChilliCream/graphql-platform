namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>b</c> subgraph
/// (<c>type Product @key(fields: "id") { id: ID @external, category: Category @shareable }</c>).
/// Subgraph <c>b</c> only owns the shareable <c>category</c> field;
/// <c>id</c> is external. The reference resolver always pins the inline
/// <c>Category.id</c> to <c>"3"</c> (matching the audit fixture), which
/// then becomes the <c>@key</c> the gateway uses to fetch
/// <c>Category.name</c> from subgraph <c>c</c>.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public Category? Category { get; init; }
}
