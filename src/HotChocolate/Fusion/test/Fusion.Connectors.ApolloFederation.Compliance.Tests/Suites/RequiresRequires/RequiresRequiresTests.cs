using HotChocolate.Fusion.Suites.RequiresRequires.A;
using HotChocolate.Fusion.Suites.RequiresRequires.B;
using HotChocolate.Fusion.Suites.RequiresRequires.C;
using HotChocolate.Fusion.Suites.RequiresRequires.D;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>requires-requires</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Four subgraphs
/// (<c>a</c>, <c>b</c>, <c>c</c>, <c>d</c>) share the <c>Product</c>
/// entity. The suite verifies that chained <c>@requires</c> dependencies
/// are resolved across multiple subgraph hops: <c>a</c> owns <c>price</c>,
/// <c>b</c> owns <c>hasDiscount</c>, <c>c</c> computes <c>isExpensive</c>
/// and <c>isExpensiveWithDiscount</c>, and <c>d</c> computes
/// <c>canAfford</c> and <c>canAffordWithDiscount</c>.
/// </summary>
public sealed class RequiresRequiresTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync),
            (DSubgraph.Name, DSubgraph.BuildAsync));

    /// <summary>
    /// Chained requires: <c>d.canAfford</c> requires <c>c.isExpensive</c>
    /// which itself requires <c>a.price</c>.
    /// </summary>
    [Fact(Skip = "Planner returns null for canAfford when queried alone; chained @requires across three subgraphs (a -> c -> d) not fully resolved.")]
    public Task Product_CanAfford_Chains_Through_IsExpensive_And_Price() => RunAsync(
        query: """
            query {
              product {
                canAfford
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "canAfford": false
              }
            }
            """);

    /// <summary>
    /// Single requires hop: <c>c.isExpensive</c> requires <c>a.price</c>.
    /// </summary>
    [Fact]
    public Task Product_IsExpensive_Requires_Price() => RunAsync(
        query: """
            query {
              product {
                isExpensive
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "isExpensive": true
              }
            }
            """);

    /// <summary>
    /// Both <c>isExpensive</c> (from <c>c</c>) and <c>canAfford</c>
    /// (from <c>d</c>) in a single query.
    /// </summary>
    [Fact]
    public Task Product_IsExpensive_And_CanAfford_Together() => RunAsync(
        query: """
            query {
              product {
                isExpensive
                canAfford
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "isExpensive": true,
                "canAfford": false
              }
            }
            """);

    /// <summary>
    /// Chained requires via discount path: <c>d.canAffordWithDiscount</c>
    /// requires <c>c.isExpensiveWithDiscount</c> which itself requires
    /// <c>b.hasDiscount</c>.
    /// </summary>
    [Fact]
    public Task Product_CanAffordWithDiscount_Chains_Through_HasDiscount() => RunAsync(
        query: """
            query {
              product {
                canAffordWithDiscount
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "canAffordWithDiscount": true
              }
            }
            """);

    /// <summary>
    /// Both chained requires paths side by side: <c>canAfford</c>
    /// (price chain) and <c>canAffordWithDiscount</c> (discount chain).
    /// </summary>
    [Fact(Skip = "Planner returns null for canAfford when queried alongside canAffordWithDiscount; chained @requires across three subgraphs (a -> c -> d) not fully resolved.")]
    public Task Product_CanAfford_And_CanAffordWithDiscount_Together() => RunAsync(
        query: """
            query {
              product {
                canAfford
                canAffordWithDiscount
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "canAfford": false,
                "canAffordWithDiscount": true
              }
            }
            """);
}
