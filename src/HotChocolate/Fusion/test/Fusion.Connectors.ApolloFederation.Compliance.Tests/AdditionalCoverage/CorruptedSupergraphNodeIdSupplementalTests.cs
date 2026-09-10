using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;
using HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class CorruptedSupergraphNodeIdSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task Gateway_Should_NotCallSource_When_NodeSelectionCannotBePlanned()
    {
        var testCase = AuditFixture.GetOfficialV2Case("corrupted-supergraph-node-id/005");
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<CorruptedSupergraphNodeIdTests>(
                capture,
                (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
                (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        await ComplianceTestBase.ExecuteAndAssertAsync(
            gateway,
            testCase,
            timeout.Token);

        Assert.Empty(capture.Requests);
    }
}
