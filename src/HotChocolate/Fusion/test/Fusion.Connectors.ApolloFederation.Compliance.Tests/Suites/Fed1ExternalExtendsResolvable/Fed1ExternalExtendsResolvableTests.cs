using HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;
using HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>fed1-external-extends-resolvable</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. The gateway composes two
/// Apollo Federation subgraphs: <c>a</c> owns the <c>Product</c> entity
/// (<c>@key(fields: "id")</c>, fields <c>name</c> / <c>pid</c>) and <c>b</c>
/// extends it (<c>@extends</c>) with a resolvable <c>price</c> field keyed by
/// the external <c>id name</c> and <c>upc</c> routing fields.
/// </summary>
[Trait("Category", "OfficialV1")]
public sealed class Fed1ExternalExtendsResolvableTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// Queries <c>productInA</c> (owned by subgraph <c>a</c>) for fields that span
    /// both subgraphs, requiring the planner to route through the external
    /// <c>id name</c> / <c>upc</c> keys to fetch <c>price</c> and <c>upc</c> from
    /// subgraph <c>b</c>.
    /// </summary>
    [Fact]
    public Task ProductInA_ResolvesExternalExtendedFields() => RunAsync(
        query: """
            query {
              productInA {
                id
                pid
                price
                upc
                name
              }
            }
            """,
        expectedData: """
            {
              "productInA": {
                "id": "p1",
                "pid": "p1-pid",
                "price": 12.3,
                "upc": "upc1",
                "name": "p1-name"
              }
            }
            """);
}
