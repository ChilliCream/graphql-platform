using HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.A;
using HotChocolate.Fusion.Suites.RequiresWithArgumentConflict.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-with-argument-conflict")]
public sealed class RequiresWithArgumentConflictTests
    : OfficialV2ComplianceTestBase<RequiresWithArgumentConflictTests>
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
