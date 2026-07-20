using HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.A;
using HotChocolate.Fusion.Suites.InterfaceObjectWithRequires.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("interface-object-with-requires")]
public sealed class InterfaceObjectWithRequiresTests
    : OfficialV2ComplianceTestBase<InterfaceObjectWithRequiresTests>
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
