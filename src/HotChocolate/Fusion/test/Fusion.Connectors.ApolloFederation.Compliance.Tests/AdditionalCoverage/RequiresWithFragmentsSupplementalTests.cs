using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.RequiresWithFragments.A;
using HotChocolate.Fusion.Suites.RequiresWithFragments.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class RequiresWithFragmentsSupplementalTests : ComplianceTestBase
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => FusionGatewayBuilder.ComposeOfficialV2Async<RequiresWithFragmentsTests>(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Fact]
    [Trait("Category", "Supplemental")]
    public Task Execute_Should_NotMaskPublicAncestor_When_InaccessibleAbstractValueIsInternalOnly()
        => RunAsync(
            """
            {
              b {
                requirer
              }
            }
            """,
            """
            {"b":{"requirer":"b1-foo_requirer"}}
            """,
            expectsErrors: false);
}
