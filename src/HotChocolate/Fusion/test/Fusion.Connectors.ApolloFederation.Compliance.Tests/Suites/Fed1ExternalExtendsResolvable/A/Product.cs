namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>a</c> subgraph
/// (<c>@key(fields: "id")</c>, owns <c>name</c> and <c>pid</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string? Name { get; init; }

    public string? Pid { get; init; }
}
