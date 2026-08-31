using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;
using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.B;
using HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("interface-object-indirect-extension")]
public sealed class InterfaceObjectIndirectExtensionTests
    : OfficialV2ComplianceTestBase<InterfaceObjectIndirectExtensionTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
