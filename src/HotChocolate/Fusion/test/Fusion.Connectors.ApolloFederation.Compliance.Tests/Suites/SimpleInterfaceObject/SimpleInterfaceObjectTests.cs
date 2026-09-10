using HotChocolate.Fusion.Suites.SimpleInterfaceObject.A;
using HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;
using HotChocolate.Fusion.Suites.SimpleInterfaceObject.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("simple-interface-object")]
public sealed class SimpleInterfaceObjectTests
    : OfficialV2ComplianceTestBase<SimpleInterfaceObjectTests>
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
