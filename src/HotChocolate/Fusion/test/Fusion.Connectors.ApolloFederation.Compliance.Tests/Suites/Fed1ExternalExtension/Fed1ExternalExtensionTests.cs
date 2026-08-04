using HotChocolate.Fusion.Suites.Fed1ExternalExtension.A;
using HotChocolate.Fusion.Suites.Fed1ExternalExtension.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV1Suite("fed1-external-extension")]
public sealed class Fed1ExternalExtensionTests
    : OfficialV1ComplianceTestBase<Fed1ExternalExtensionTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV1Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV1")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV1CaseAsync(caseId);
}
