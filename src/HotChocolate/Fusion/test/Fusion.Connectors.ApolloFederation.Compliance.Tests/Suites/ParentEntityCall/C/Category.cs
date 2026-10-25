namespace HotChocolate.Fusion.Suites.ParentEntityCall.C;

/// <summary>
/// The <c>Category</c> value type as projected by the <c>c</c> subgraph
/// (<c>type Category { details: CategoryDetails }</c>). Not an entity in
/// this subgraph: there is no <c>@key</c>, so <c>details</c> is only
/// reachable via the parent <c>Product.category</c> field.
/// </summary>
public sealed class Category
{
    public CategoryDetails? Details { get; init; }
}

/// <summary>
/// The <c>CategoryDetails</c> value type as projected by the <c>c</c>
/// subgraph (<c>type CategoryDetails { products: Int }</c>).
/// </summary>
public sealed class CategoryDetails
{
    public int? Products { get; init; }
}
