using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("provides-on-interface")]
public sealed class ProvidesOnInterfaceTests
    : OfficialV2ComplianceTestBase<ProvidesOnInterfaceTests>
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
