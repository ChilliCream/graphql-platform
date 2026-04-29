namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Price;

/// <summary>
/// The <c>Category</c> entity as projected by the <c>price</c> subgraph
/// (<c>@key(fields: "id tag")</c>).
/// </summary>
public sealed class Category
{
    public string Id { get; init; } = default!;

    public string? Tag { get; init; }
}
