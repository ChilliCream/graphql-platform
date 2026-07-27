using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnUnion.SubgraphC;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("provides-on-union")]
public sealed class ProvidesOnUnionTests
    : OfficialV2ComplianceTestBase<ProvidesOnUnionTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync),
            (SubgraphCSubgraph.Name, SubgraphCSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
