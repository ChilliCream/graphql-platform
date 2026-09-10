namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

/// <summary>
/// The <c>Product</c> entity as projected by the <c>b</c> subgraph. The subgraph
/// extends the federated <c>Product</c> (<c>@extends</c> in the audit SDL) by
/// adding the local <c>price</c> field while keeping <c>id</c>, <c>name</c> and
/// <c>upc</c> external entity-routing keys.
/// </summary>
public sealed class Product
{
    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? Upc { get; init; }

    public double Price { get; init; }
}
