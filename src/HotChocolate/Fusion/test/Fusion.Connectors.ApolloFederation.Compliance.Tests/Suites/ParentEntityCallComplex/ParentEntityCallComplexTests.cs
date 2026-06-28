using HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.B;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.C;
using HotChocolate.Fusion.Suites.ParentEntityCallComplex.D;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>parent-entity-call-complex</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Four Apollo Federation
/// subgraphs cooperate on a single <c>Product</c> request: subgraph
/// <c>d</c> owns the root <c>productFromD(id)</c> and <c>Product.name</c>;
/// subgraphs <c>a</c> and <c>b</c> both contribute the shareable
/// <c>Product.category</c> field by inlining a <c>Category</c> value type
/// (<c>a</c> sets <c>details</c>, <c>b</c> sets the shareable <c>id</c>);
/// subgraph <c>c</c> owns the <c>Category @key("id")</c> entity and
/// projects <c>Category.name</c> via the <c>__resolveReference</c> entity
/// call. The supergraph composes <c>category { id name details }</c> by
/// merging the inline contributions from <c>a</c> and <c>b</c> with the
/// downstream entity call into <c>c</c>.
/// </summary>
public sealed class ParentEntityCallComplexTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync),
            (DSubgraph.Name, DSubgraph.BuildAsync));

    /// <summary>
    /// The single audit case: requests <c>Product.category { id name details }</c>
    /// from the <c>d</c> root. <c>id</c> comes from <c>b</c>, <c>name</c> from
    /// the <c>c</c> entity lookup keyed on that id, and <c>details</c> from
    /// the parent <c>Product</c> entity call into <c>a</c>.
    /// </summary>
    [Fact]
    public Task ProductFromD_Composes_Category_Fields_Across_Four_Subgraphs() => RunAsync(
        query: """
            query {
              productFromD(id: "1") {
                id
                name
                category {
                  id
                  name
                  details
                }
              }
            }
            """,
        expectedData: """
            {
              "productFromD": {
                "id": "1",
                "name": "Product#1",
                "category": {
                  "id": "3",
                  "name": "Category#3",
                  "details": "Details for Product#1"
                }
              }
            }
            """);
}
