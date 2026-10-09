namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

/// <summary>
/// The <c>Oven</c> entity as projected by subgraph <c>b</c>.
/// Implements <c>Node</c> and <c>WithWarranty</c> in this subgraph.
/// </summary>
public sealed class Oven : INode, IWithWarranty
{
    public string Id { get; init; } = default!;
    public int? Warranty { get; init; }
}
