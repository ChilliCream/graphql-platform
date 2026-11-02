namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>list</c> subgraph
/// (<c>@key(fields: "id pid")</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string? Pid { get; init; }
}
