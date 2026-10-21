using HotChocolate.Execution;
using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphA;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphB;
using HotChocolate.Fusion.Suites.ProvidesOnInterface.SubgraphC;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class ProvidesOnInterfaceSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task ProvidedInterfacePath_Should_RouteUnprovidedFieldToOwningSubgraph()
    {
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<ProvidesOnInterfaceTests>(
                capture,
                (SubgraphASubgraph.Name, SubgraphASubgraph.BuildAsync),
                (SubgraphBSubgraph.Name, SubgraphBSubgraph.BuildAsync),
                (SubgraphCSubgraph.Name, SubgraphCSubgraph.BuildAsync));
        var testCase = AuditFixture.GetOfficialV2Case<ProvidesOnInterfaceTests>(
            "provides-on-interface/001");

        var result = await gateway.Executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(testCase.Query).Build(),
            TestContext.Current.CancellationToken);

        AuditAssertions.Assert(
            result.ToJson(),
            testCase.ExpectedData,
            testCase.HasExpectedErrors ? testCase.ExpectsErrors : null);

        await ApolloIntegrationSnapshot.MatchAsync(
            new Snapshot(extension: ".yaml"),
            testCase.Query,
            capture,
            result,
            includeRequestOrder: true);
    }
}
