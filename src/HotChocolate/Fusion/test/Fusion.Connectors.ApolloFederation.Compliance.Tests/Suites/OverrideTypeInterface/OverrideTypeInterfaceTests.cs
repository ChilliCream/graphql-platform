using HotChocolate.Fusion.Suites.OverrideTypeInterface.A;
using HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("override-type-interface")]
public sealed class OverrideTypeInterfaceTests
    : OfficialV2ComplianceTestBase<OverrideTypeInterfaceTests>
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
