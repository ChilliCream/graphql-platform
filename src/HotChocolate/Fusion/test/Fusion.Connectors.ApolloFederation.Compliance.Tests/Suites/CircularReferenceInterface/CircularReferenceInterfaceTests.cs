using HotChocolate.Fusion.Suites.CircularReferenceInterface.A;
using HotChocolate.Fusion.Suites.CircularReferenceInterface.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("circular-reference-interface")]
public sealed class CircularReferenceInterfaceTests
    : OfficialV2ComplianceTestBase<CircularReferenceInterfaceTests>
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
