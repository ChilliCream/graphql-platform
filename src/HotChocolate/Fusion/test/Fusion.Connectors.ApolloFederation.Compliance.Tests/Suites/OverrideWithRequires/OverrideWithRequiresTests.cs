using HotChocolate.Fusion.Suites.OverrideWithRequires.A;
using HotChocolate.Fusion.Suites.OverrideWithRequires.B;
using HotChocolate.Fusion.Suites.OverrideWithRequires.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("override-with-requires")]
public sealed class OverrideWithRequiresTests
    : OfficialV2ComplianceTestBase<OverrideWithRequiresTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
