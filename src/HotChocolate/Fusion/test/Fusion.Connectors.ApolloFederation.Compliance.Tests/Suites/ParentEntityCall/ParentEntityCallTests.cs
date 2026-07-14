using HotChocolate.Fusion.Suites.ParentEntityCall.A;
using HotChocolate.Fusion.Suites.ParentEntityCall.B;
using HotChocolate.Fusion.Suites.ParentEntityCall.C;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("parent-entity-call")]
public sealed class ParentEntityCallTests
    : OfficialV2ComplianceTestBase<ParentEntityCallTests>
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
