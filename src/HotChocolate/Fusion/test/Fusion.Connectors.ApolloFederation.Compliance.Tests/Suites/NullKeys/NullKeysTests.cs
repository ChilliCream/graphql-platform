using HotChocolate.Fusion.Suites.NullKeys.A;
using HotChocolate.Fusion.Suites.NullKeys.B;
using HotChocolate.Fusion.Suites.NullKeys.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("null-keys")]
public sealed class NullKeysTests
    : OfficialV2ComplianceTestBase<NullKeysTests>
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
