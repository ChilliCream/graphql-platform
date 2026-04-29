namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;

/// <summary>
/// The <c>Category</c> value type as projected by the <c>a</c> subgraph
/// (<c>type Category { details: String }</c>). Not an entity in this
/// subgraph: it has no <c>@key</c> and is only ever produced inline by
/// the parent <c>Product.category</c> field.
/// </summary>
public sealed class Category
{
    public string? Details { get; init; }
}
