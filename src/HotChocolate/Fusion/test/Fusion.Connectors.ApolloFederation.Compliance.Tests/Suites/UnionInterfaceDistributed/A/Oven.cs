namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// The <c>Oven</c> entity as projected by subgraph <c>a</c>.
/// In this subgraph, Oven does not implement Node or WithWarranty.
/// </summary>
public sealed class Oven : IProduct
{
    public string Id { get; init; } = default!;
    public int? Warranty { get; init; }
}
