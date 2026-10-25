namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;

/// <summary>
/// The <c>Toaster</c> entity as projected by subgraph <c>a</c>.
/// Implements <c>Node</c> and <c>WithWarranty</c>.
/// </summary>
public sealed class Toaster : INode, IWithWarranty, IProduct
{
    public string Id { get; init; } = default!;
    public int? Warranty { get; init; }
}
