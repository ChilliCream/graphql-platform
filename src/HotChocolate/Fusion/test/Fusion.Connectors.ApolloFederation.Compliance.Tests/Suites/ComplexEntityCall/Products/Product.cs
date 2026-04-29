namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>products</c> subgraph
/// (<c>@extends @key(fields: "id")</c>, <c>id</c> external, owns <c>category</c>).
/// </summary>
public sealed class Product
{
    public string Id { get; init; } = default!;

    public string CategoryId { get; init; } = default!;
}
