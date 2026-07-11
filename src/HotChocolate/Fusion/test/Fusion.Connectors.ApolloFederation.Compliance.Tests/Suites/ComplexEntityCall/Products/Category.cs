namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>products</c> subgraph
/// (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string? Tag { get; init; }

    public string MainProductId { get; init; } = default!;
}
