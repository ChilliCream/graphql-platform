using HotChocolate.Fusion.Suites.SimpleEntityCall.Email;
using HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("simple-entity-call")]
public sealed class SimpleEntityCallTests
    : OfficialV2ComplianceTestBase<SimpleEntityCallTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (EmailSubgraph.Name, EmailSubgraph.BuildAsync),
            (NicknameSubgraph.Name, NicknameSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
