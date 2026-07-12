using HotChocolate.Fusion.Suites.InputObjectIntersection.A;
using HotChocolate.Fusion.Suites.InputObjectIntersection.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("input-object-intersection")]
public sealed class InputObjectIntersectionTests
    : OfficialV2ComplianceTestBase<InputObjectIntersectionTests>
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
