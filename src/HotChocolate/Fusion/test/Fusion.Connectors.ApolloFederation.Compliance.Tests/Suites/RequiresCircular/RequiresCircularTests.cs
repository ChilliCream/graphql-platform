using HotChocolate.Fusion.Suites.RequiresCircular.A;
using HotChocolate.Fusion.Suites.RequiresCircular.B;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>requires-circular</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Two subgraphs
/// (<c>a</c>, <c>b</c>) share <c>Post</c> and <c>Author</c> entities.
/// The suite verifies circular <c>@requires</c> resolution:
/// subgraph <c>b</c> computes <c>byNovice</c> from
/// <c>author { yearsOfExperience }</c> (owned by <c>a</c>),
/// and subgraph <c>a</c> computes <c>byExpert</c> from <c>byNovice</c>
/// (owned by <c>b</c>).
/// </summary>
public sealed class RequiresCircularTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    /// <summary>
    /// <c>b.byNovice</c> requires <c>author { yearsOfExperience }</c>
    /// from subgraph <c>a</c>. John (5 years) is a novice, Jane (20 years) is not.
    /// </summary>
    [Fact(Skip = "Satisfiability validator cannot resolve circular @requires chains across subgraphs.")]
    public Task Feed_ByNovice_Requires_Author_YearsOfExperience() => RunAsync(
        query: """
            {
              feed {
                byNovice
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                { "byNovice": true },
                { "byNovice": false }
              ]
            }
            """);

    /// <summary>
    /// Circular requires: <c>a.byExpert</c> requires <c>b.byNovice</c>,
    /// which itself requires <c>a.author { yearsOfExperience }</c>.
    /// </summary>
    [Fact(Skip = "Satisfiability validator cannot resolve circular @requires chains across subgraphs.")]
    public Task Feed_ByExpert_Chains_Through_ByNovice_And_Author() => RunAsync(
        query: """
            {
              feed {
                byExpert
              }
            }
            """,
        expectedData: """
            {
              "feed": [
                { "byExpert": false },
                { "byExpert": true }
              ]
            }
            """);
}
