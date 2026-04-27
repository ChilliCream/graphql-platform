namespace HotChocolate.Fusion.Suites.NestedProvides.Category;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>category</c>
/// subgraph (<c>@key(fields: "id")</c>). Carries a list of associated
/// <see cref="CategoryEntity"/> items that are marked <c>@external</c>
/// but provided inline by <c>Query.products</c>.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public List<CategoryEntity>? Categories { get; init; }
}
