using HotChocolate.Fusion.Suites.RequiresInterface.A;
using HotChocolate.Fusion.Suites.RequiresInterface.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-interface")]
public sealed class RequiresInterfaceTests
    : OfficialV2ComplianceTestBase<RequiresInterfaceTests>
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
