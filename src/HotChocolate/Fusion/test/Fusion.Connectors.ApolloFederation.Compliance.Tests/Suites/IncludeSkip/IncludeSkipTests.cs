using HotChocolate.Fusion.Suites.IncludeSkip.A;
using HotChocolate.Fusion.Suites.IncludeSkip.B;
using HotChocolate.Fusion.Suites.IncludeSkip.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("include-skip")]
public sealed class IncludeSkipTests
    : OfficialV2ComplianceTestBase<IncludeSkipTests>
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
