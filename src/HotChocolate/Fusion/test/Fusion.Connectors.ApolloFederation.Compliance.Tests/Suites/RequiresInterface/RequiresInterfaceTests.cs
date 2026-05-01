using HotChocolate.Fusion.Suites.RequiresInterface.A;
using HotChocolate.Fusion.Suites.RequiresInterface.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>requires-interface</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Subgraph <c>a</c>
/// defines <c>User.city</c> with <c>@requires(fields: "address { id }")</c>
/// and <c>User.country</c> with
/// <c>@requires(fields: "address { ... on WorkAddress { id } }")</c>.
/// Subgraph <c>b</c> owns <c>User.address</c> (shareable).
/// </summary>
public sealed class RequiresInterfaceTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact(Skip = "Planner does not yet satisfy input-object @require arguments for nested field selections.")]
    public Task A_City_Requires_Address_Id() => RunAsync(
        query: """
            {
              a {
                city
              }
            }
            """,
        expectedData: """
            {
              "a": { "city": "a1-city" }
            }
            """);

    [Fact(Skip = "Planner does not yet satisfy input-object @require arguments for nested field selections.")]
    public Task B_City_Requires_Address_Id_From_Other_Subgraph() => RunAsync(
        query: """
            {
              b {
                city
              }
            }
            """,
        expectedData: """
            {
              "b": { "city": "a2-city" }
            }
            """);

    [Fact]
    public Task A_Country_Returns_Null_When_Address_Is_Not_WorkAddress() => RunAsync(
        query: """
            {
              a {
                country
              }
            }
            """,
        expectedData: """
            {
              "a": { "country": null }
            }
            """);

    [Fact]
    public Task A_Address_Returns_Typename_And_Id() => RunAsync(
        query: """
            {
              a {
                address {
                  __typename
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "a": { "address": { "__typename": "HomeAddress", "id": "a1" } }
            }
            """);

    [Fact]
    public Task B_Address_Returns_Typename_And_Id() => RunAsync(
        query: """
            {
              b {
                address {
                  __typename
                  id
                }
              }
            }
            """,
        expectedData: """
            {
              "b": { "address": { "__typename": "WorkAddress", "id": "a2" } }
            }
            """);
}
