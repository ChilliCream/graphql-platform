namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.C;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>c</c> subgraph
/// (<c>type Category @key(fields: "id") { id: ID, name: String }</c>).
/// Owns the <c>name</c> field; the <c>__resolveReference</c> resolver
/// builds <c>Category#{id}</c> for any requested id.
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }
}
