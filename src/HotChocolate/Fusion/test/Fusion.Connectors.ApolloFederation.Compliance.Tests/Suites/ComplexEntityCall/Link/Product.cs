namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Link;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>link</c> subgraph
/// (<c>@key(fields: "id")</c>, <c>@key(fields: "id pid")</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Pid { get; init; } = default!;
}
