using HotChocolate.Fusion.Suites.EnumIntersection.A;
using HotChocolate.Fusion.Suites.EnumIntersection.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("enum-intersection")]
public sealed class EnumIntersectionTests
    : OfficialV2ComplianceTestBase<EnumIntersectionTests>
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
