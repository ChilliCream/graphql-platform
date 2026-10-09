using HotChocolate.Fusion.Suites.ChildTypeMismatch.A;
using HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("child-type-mismatch")]
public sealed class ChildTypeMismatchTests
    : OfficialV2ComplianceTestBase<ChildTypeMismatchTests>
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
