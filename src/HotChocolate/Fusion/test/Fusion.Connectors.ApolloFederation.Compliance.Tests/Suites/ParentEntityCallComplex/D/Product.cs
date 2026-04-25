namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>d</c> subgraph
/// (<c>type Product @key(fields: "id") { id: ID, name: String }</c>).
/// Owns the <c>name</c> field; the <c>__resolveReference</c> resolver
/// builds <c>Product#{id}</c> for any requested id.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
