using HotChocolate.Fusion.Suites.RequiresWithFragments.A;
using HotChocolate.Fusion.Suites.RequiresWithFragments.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>requires-with-fragments</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Subgraph <c>a</c> owns
/// <c>Entity.data: Foo</c> (with Baz and Qux implementations).
/// Subgraph <c>b</c> defines <c>Entity.requirer</c> with complex
/// <c>@requires</c> using inline fragments on <c>Bar</c>, <c>Baz</c>,
/// and <c>Qux</c>, and <c>Entity.requirer2</c> with
/// <c>@requires(fields: "data { ... on Foo { foo } }")</c>.
/// </summary>
public sealed class RequiresWithFragmentsTests : ComplianceTestBase
{
    private const string SkipReason =
        "Composition rejects nested @requires with inline fragments as invalid syntax.";

    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact(Skip = SkipReason)]
    public Task B_And_Bb_Data_Typename() => RunAsync(
        query: """
            {
              b {
                data {
                  __typename
                }
              }
              bb {
                data {
                  __typename
                }
              }
            }
            """,
        expectedData: """
            {
              "b": { "data": { "__typename": "Baz" } },
              "bb": { "data": { "__typename": "Qux" } }
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task A_Requirer() => RunAsync(
        query: """
            {
              a {
                requirer
              }
            }
            """,
        expectedData: """
            {
              "a": { "requirer": "q1-foo_requirer" }
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task A_Data_Typename_And_Requirer() => RunAsync(
        query: """
            {
              a {
                data {
                  __typename
                }
                requirer
              }
            }
            """,
        expectedData: """
            {
              "a": { "data": { "__typename": "Qux" }, "requirer": "q1-foo_requirer" }
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Bb_Data_Typename_And_Requirer() => RunAsync(
        query: """
            {
              bb {
                data {
                  __typename
                }
                requirer
              }
            }
            """,
        expectedData: """
            {
              "bb": { "data": { "__typename": "Qux" }, "requirer": "q1-foo_requirer" }
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task B_Data_Typename_And_Requirer() => RunAsync(
        query: """
            {
              b {
                data {
                  __typename
                }
                requirer
              }
            }
            """,
        expectedData: """
            {
              "b": { "data": { "__typename": "Baz" }, "requirer": "b1-foo_requirer" }
            }
            """);

    [Fact(Skip = SkipReason)]
    public Task Bb_Data_Typename_And_Requirer2() => RunAsync(
        query: """
            {
              bb {
                data {
                  __typename
                }
                requirer2
              }
            }
            """,
        expectedData: """
            {
              "bb": { "data": { "__typename": "Qux" }, "requirer2": "q1-foo_requirer2" }
            }
            """);
}
