using HotChocolate.Fusion.Suites.RequiresCircular.A;
using HotChocolate.Fusion.Suites.RequiresCircular.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-circular")]
public sealed class RequiresCircularTests
    : OfficialV2ComplianceTestBase<RequiresCircularTests>
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
}
