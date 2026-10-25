namespace HotChocolate.Fusion.Suites.RequiresWithArgument.A;

/// <summary>
/// The <c>Category</c> type referenced externally by the <c>a</c>
/// subgraph. Its <c>averagePrice</c> field is populated by the
/// federation external setter when the gateway attaches the requires
/// dependencies to the entity representation.
/// </summary>
public sealed class Category
{
    public int? AveragePrice { get; set; }
}
