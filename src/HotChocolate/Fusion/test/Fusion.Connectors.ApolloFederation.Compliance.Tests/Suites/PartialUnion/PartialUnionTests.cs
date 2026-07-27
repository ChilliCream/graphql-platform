using HotChocolate.Fusion.Suites.PartialUnion.A;
using HotChocolate.Fusion.Suites.PartialUnion.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("partial-union")]
public sealed class PartialUnionTests
    : OfficialV2ComplianceTestBase<PartialUnionTests>
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
