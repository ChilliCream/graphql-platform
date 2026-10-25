using HotChocolate.Fusion.Suites.UnionIntersection.A;
using HotChocolate.Fusion.Suites.UnionIntersection.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("union-intersection")]
public sealed class UnionIntersectionTests
    : OfficialV2ComplianceTestBase<UnionIntersectionTests>
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
