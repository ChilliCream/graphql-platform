using HotChocolate.Fusion.Suites.RequiresWithFragments.A;
using HotChocolate.Fusion.Suites.RequiresWithFragments.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-with-fragments")]
public sealed class RequiresWithFragmentsTests
    : OfficialV2ComplianceTestBase<RequiresWithFragmentsTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);

    [Fact]
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
