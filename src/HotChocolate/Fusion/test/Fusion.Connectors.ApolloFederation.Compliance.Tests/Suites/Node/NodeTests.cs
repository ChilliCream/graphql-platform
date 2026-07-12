using HotChocolate.Fusion.Suites.Node.Node;
using HotChocolate.Fusion.Suites.Node.NodeTwo;
using HotChocolate.Fusion.Suites.Node.Types;

namespace HotChocolate.Fusion.Suites;

[OfficialV2Suite("node")]
public sealed class NodeTests
    : OfficialV2ComplianceTestBase<NodeTests>
{
    protected override Task<FusionGateway> BuildGatewayAsync()
        => ComposeOfficialV2Async(
            (NodeSubgraph.Name, NodeSubgraph.BuildAsync),
            (NodeTwoSubgraph.Name, NodeTwoSubgraph.BuildAsync),
            (TypesSubgraph.Name, TypesSubgraph.BuildAsync));

    [Theory]
    [MemberData(nameof(Cases))]
    [Trait("Category", "OfficialV2")]
    public Task OfficialCase_Should_MatchExpectedResult_When_Executed(string caseId)
        => RunOfficialV2CaseAsync(caseId);
}
