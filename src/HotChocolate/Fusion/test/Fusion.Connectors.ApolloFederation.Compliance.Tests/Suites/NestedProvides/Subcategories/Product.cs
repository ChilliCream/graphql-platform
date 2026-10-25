namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>subcategories</c>
/// subgraph (<c>@key(fields: "id")</c>). Carries a shareable list of
/// associated <see cref="CategoryEntity"/> items.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public List<CategoryEntity>? Categories { get; init; }
}
