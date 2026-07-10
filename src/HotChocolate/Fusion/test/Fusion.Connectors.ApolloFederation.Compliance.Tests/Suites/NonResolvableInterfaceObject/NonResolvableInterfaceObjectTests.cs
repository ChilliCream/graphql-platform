using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>non-resolvable-interface-object</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two subgraphs exercise the
/// <c>@interfaceObject</c> directive paired with non-resolvable <c>@key</c>:
/// subgraph <c>a</c> owns the <c>Node</c> interface entity and a non-resolvable
/// <c>Product</c> interface object, while subgraph <c>b</c> owns a non-resolvable
/// <c>Node</c> interface object (contributing <c>field</c>) and the
/// <c>Product</c> interface entity implemented by <c>Bread</c>.
/// </summary>
public sealed class NonResolvableInterfaceObjectTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    public Task B_ReturnsIdAndField() => RunAsync(
        query: """
            query {
              b {
                id
                field
              }
            }
            """,
        expectedData: """
            {
              "b": {
                "id": "n1",
                "field": "foo"
              }
            }
            """);

    [Fact]
    public Task A_Field_Errors_WhenInterfaceObjectNotResolvable() => RunAsync(
        query: """
            query {
              a {
                field
              }
            }
            """,
        expectedData: null,
        expectsErrors: true);

    [Fact]
    public Task B_ReturnsId() => RunAsync(
        query: """
            query {
              b {
                id
              }
            }
            """,
        expectedData: """
            {
              "b": {
                "id": "n1"
              }
            }
            """);

    [Fact]
    public Task A_Id_ReturnsNullWithError() => RunAsync(
        query: """
            query {
              a {
                id
              }
            }
            """,
        expectedData: """
            {
              "a": null
            }
            """,
        expectsErrors: true);

    [Fact]
    public Task Product_ReturnsId() => RunAsync(
        query: """
            query {
              product {
                id
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "id": "p1"
              }
            }
            """);

    [Fact]
    public Task Product_IdAndName_Errors_WhenNotResolvable() => RunAsync(
        query: """
            query {
              product {
                id
                name
              }
            }
            """,
        expectedData: null,
        expectsErrors: true);

    [Fact]
    public Task Product_BreadFragment_ReturnsId() => RunAsync(
        query: """
            query {
              product {
                ... on Bread {
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "id": "p1"
              }
            }
            """);
}
