using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Suites.UnionInterfaceDistributed.A;
using HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("union-interface-distributed", NodeResolution = NodeResolution.SourceSchema)]
public sealed class UnionInterfaceDistributedTests
    : OfficialV2ComplianceTestBase<UnionInterfaceDistributedTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
            (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
