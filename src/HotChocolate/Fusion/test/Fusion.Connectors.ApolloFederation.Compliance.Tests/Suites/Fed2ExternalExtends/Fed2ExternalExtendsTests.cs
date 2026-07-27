using HotChocolate.Fusion.Suites.Fed2ExternalExtends.A;
using HotChocolate.Fusion.Suites.Fed2ExternalExtends.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("fed2-external-extends")]
public sealed class Fed2ExternalExtendsTests
    : OfficialV2ComplianceTestBase<Fed2ExternalExtendsTests>
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
