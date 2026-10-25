using HotChocolate.Fusion.Suites.RequiresRequires.A;
using HotChocolate.Fusion.Suites.RequiresRequires.B;
using HotChocolate.Fusion.Suites.RequiresRequires.C;
using HotChocolate.Fusion.Suites.RequiresRequires.D;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("requires-requires")]
public sealed class RequiresRequiresTests
    : OfficialV2ComplianceTestBase<RequiresRequiresTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (ASubgraph.Name, ASubgraph.BuildAsync),
            (BSubgraph.Name, BSubgraph.BuildAsync),
            (CSubgraph.Name, CSubgraph.BuildAsync),
            (DSubgraph.Name, DSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
