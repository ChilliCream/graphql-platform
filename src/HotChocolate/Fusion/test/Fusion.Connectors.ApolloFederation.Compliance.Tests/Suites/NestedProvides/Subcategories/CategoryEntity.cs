namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>subcategories</c>
/// subgraph (<c>@key(fields: "id")</c>). Carries a shareable list of
/// sub-category references.
/// </summary>
public sealed class CategoryEntity
{
    public string Id { get; init; } = default!;

    public List<CategoryEntity>? SubCategories { get; init; }
}
