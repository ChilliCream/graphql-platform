using HotChocolate.Fusion.Suites.KeysMashup.A;
using HotChocolate.Fusion.Suites.KeysMashup.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("keys-mashup")]
public sealed class KeysMashupTests
    : OfficialV2ComplianceTestBase<KeysMashupTests>
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
