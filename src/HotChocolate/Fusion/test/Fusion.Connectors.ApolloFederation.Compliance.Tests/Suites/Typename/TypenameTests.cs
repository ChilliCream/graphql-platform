using HotChocolate.Fusion.Suites.Typename.A;
using HotChocolate.Fusion.Suites.Typename.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("typename")]
public sealed class TypenameTests
    : OfficialV2ComplianceTestBase<TypenameTests>
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
