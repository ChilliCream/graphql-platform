using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;
using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("corrupted-supergraph-node-id", NodeResolution = NodeResolution.SourceSchema)]
public sealed class CorruptedSupergraphNodeIdTests
    : OfficialV2ComplianceTestBase<CorruptedSupergraphNodeIdTests>
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
