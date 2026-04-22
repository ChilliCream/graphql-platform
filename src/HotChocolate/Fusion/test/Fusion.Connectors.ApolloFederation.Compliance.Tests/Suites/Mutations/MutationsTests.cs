using HotChocolate.Fusion.Suites.Mutations.A;
using HotChocolate.Fusion.Suites.Mutations.B;
using HotChocolate.Fusion.Suites.Mutations.C;
using HotChocolate.Fusion.Suites.Mutations.Shared;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>mutations</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs (<c>a</c>, <c>b</c>, <c>c</c>) share a single mutable state
/// object so cross-subgraph mutation ordering can be verified end-to-end.
/// </summary>
public sealed class MutationsTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
    {
        var state = new MutationsState();
        return FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, () => ASubgraph.BuildAsync(state)),
            (BSubgraph.Name, () => BSubgraph.BuildAsync(state)),
            (CSubgraph.Name, () => CSubgraph.BuildAsync(state)));
    }

    /// <summary>
    /// <c>addProduct</c> in subgraph <c>a</c> creates the entity; subgraph
    /// <c>b</c> contributes <c>isExpensive</c> (via <c>@requires(price)</c>)
    /// and <c>isAvailable</c> through an entity reference.
    /// </summary>
    [Fact(Skip = "Planner does not yet route the @requires(price) field through the entity lookup, so the @requires resolver runs without its required input. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task AddProduct_Composes_From_Two_Subgraphs() => RunAsync(
        query: """
            mutation {
              addProduct(input: { name: "new", price: 599.99 }) {
                name
                price
                isExpensive
                isAvailable
              }
            }
            """,
        expectedData: """
            {
              "addProduct": {
                "name": "new",
                "price": 599.99,
                "isExpensive": true,
                "isAvailable": true
              }
            }
            """);

    /// <summary>
    /// <c>Query.product</c> in <c>a</c> returns the seeded product; the
    /// planner enriches it with <c>isExpensive</c> and <c>isAvailable</c>
    /// from <c>b</c>.
    /// </summary>
    [Fact(Skip = "Planner does not yet route the @requires(price) field through the entity lookup, so the @requires resolver runs without its required input. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Product_Composes_From_Two_Subgraphs() => RunAsync(
        query: """
            query {
              product(id: "p1") {
                id
                name
                price
                isExpensive
                isAvailable
              }
            }
            """,
        expectedData: """
            {
              "product": {
                "id": "p1",
                "name": "p1-name",
                "price": 9.99,
                "isExpensive": false,
                "isAvailable": true
              }
            }
            """);

    /// <summary>
    /// Mixed-subgraph mutation chain. The four operations route to <c>c</c>,
    /// <c>a</c>, <c>c</c>, and <c>b</c>; GraphQL requires serial execution
    /// of mutation root fields so the running tally reaches the expected
    /// final value before <c>delete</c> consumes it.
    /// </summary>
    [Fact]
    public Task Mutation_Chain_Executes_In_Order_Across_Three_Subgraphs() => RunAsync(
        query: """
            mutation {
              five: add(num: 5, requestId: "r1")
              ten: multiply(by: 2, requestId: "r1")
              twelve: add(num: 2, requestId: "r1")
              final: delete(requestId: "r1")
            }
            """,
        expectedData: """
            {
              "five": 5,
              "ten": 10,
              "twelve": 12,
              "final": 12
            }
            """);

    /// <summary>
    /// Shareable <c>addCategory</c> mutation. The planner picks one
    /// subgraph (<c>b</c> owns the <c>name</c> field) and the result
    /// includes both <c>id</c> and <c>name</c>.
    /// </summary>
    [Fact]
    public Task AddCategory_Routes_Shareable_Mutation_To_Owning_Subgraph() => RunAsync(
        query: """
            mutation {
              addCategory(name: "new", requestId: "r2") {
                id
                name
              }
            }
            """,
        expectedData: """
            {
              "addCategory": {
                "id": "c-added-r2",
                "name": "new"
              }
            }
            """);
}
