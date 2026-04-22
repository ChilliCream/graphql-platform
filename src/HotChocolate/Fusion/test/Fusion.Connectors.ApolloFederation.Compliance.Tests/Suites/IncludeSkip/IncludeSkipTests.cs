using HotChocolate.Fusion.Suites.IncludeSkip.A;
using HotChocolate.Fusion.Suites.IncludeSkip.B;
using HotChocolate.Fusion.Suites.IncludeSkip.C;

namespace HotChocolate.Fusion.Suites;

/// <summary>
/// Port of the <c>include-skip</c> suite from
/// <c>graphql-hive/federation-gateway-audit</c>. Three Apollo Federation
/// subgraphs share the <c>Product</c> entity. The audit verifies that
/// <c>@include</c> and <c>@skip</c> short-circuit downstream resolvers
/// (the <c>neverCalledInclude</c> and <c>neverCalledSkip</c> resolvers
/// throw if invoked) and that toggled fields still receive their
/// <c>@requires</c> dependencies.
/// </summary>
public sealed class IncludeSkipTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeAsync(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    /// <summary>
    /// <c>@include(if: false)</c> should keep the planner from invoking
    /// the <c>neverCalledInclude</c> resolver.
    /// </summary>
    [Fact]
    public Task NeverCalledInclude_Is_Skipped_When_Include_False() => RunAsync(
        query: """
            query ($bool: Boolean = false) {
              product {
                price
                neverCalledInclude @include(if: $bool)
              }
            }
            """,
        expectedData: """
            {
              "product": { "price": 699.99 }
            }
            """);

    /// <summary>
    /// <c>@skip(if: true)</c> should keep the planner from invoking
    /// the <c>neverCalledSkip</c> resolver.
    /// </summary>
    [Fact]
    public Task NeverCalledSkip_Is_Skipped_When_Skip_True() => RunAsync(
        query: """
            query ($bool: Boolean = true) {
              product {
                price
                neverCalledSkip @skip(if: $bool)
              }
            }
            """,
        expectedData: """
            {
              "product": { "price": 699.99 }
            }
            """);

    /// <summary>
    /// <c>@include(if: true)</c> keeps the field active. The chained
    /// <c>@requires</c> path must still deliver <c>isExpensive</c> to
    /// <c>c</c>'s <c>include</c> resolver.
    /// </summary>
    [Fact(Skip = "Planner does not yet route the @requires(isExpensive) field through the entity lookup. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Include_Resolves_Through_Requires_Chain() => RunAsync(
        query: """
            query ($bool: Boolean = true) {
              product {
                price
                include @include(if: $bool)
              }
            }
            """,
        expectedData: """
            {
              "product": { "price": 699.99, "include": true }
            }
            """);

    /// <summary>
    /// <c>@skip(if: false)</c> keeps the field active. The chained
    /// <c>@requires</c> path must still deliver <c>isExpensive</c> to
    /// <c>c</c>'s <c>skip</c> resolver.
    /// </summary>
    [Fact(Skip = "Planner does not yet route the @requires(isExpensive) field through the entity lookup. See APOLLO_FEDERATION_COMPLIANCE_FOLLOWUP.md follow-up.")]
    public Task Skip_Resolves_Through_Requires_Chain() => RunAsync(
        query: """
            query ($bool: Boolean = false) {
              product {
                price
                skip @skip(if: $bool)
              }
            }
            """,
        expectedData: """
            {
              "product": { "price": 699.99, "skip": true }
            }
            """);
}
