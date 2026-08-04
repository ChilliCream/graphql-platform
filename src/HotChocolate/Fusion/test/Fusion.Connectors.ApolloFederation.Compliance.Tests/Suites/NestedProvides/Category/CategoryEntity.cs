namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>category</c>
/// subgraph (<c>@key(fields: "id")</c>). Owns the <c>name</c> field
/// and carries <c>subCategories</c> marked <c>@external</c>.
/// </summary>
public sealed class CategoryEntity
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public List<CategoryEntity>? SubCategories { get; init; }
}
