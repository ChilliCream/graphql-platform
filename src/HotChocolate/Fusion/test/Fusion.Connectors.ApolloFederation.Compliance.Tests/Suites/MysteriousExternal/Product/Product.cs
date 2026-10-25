namespace HotChocolate.Fusion.Suites.MysteriousExternal.Product;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>product</c> subgraph
/// (<c>@key(fields: "id")</c>). Owns <c>id</c> and <c>name</c>.
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string Name { get; init; } = default!;
}
