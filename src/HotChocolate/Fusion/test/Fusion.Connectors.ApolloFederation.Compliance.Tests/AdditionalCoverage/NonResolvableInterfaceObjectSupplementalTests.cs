using HotChocolate.Execution;
using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.A;
using HotChocolate.Fusion.Suites.NonResolvableInterfaceObject.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class NonResolvableInterfaceObjectSupplementalTests
{
    [Theory]
    [InlineData("query { a { __typename } }", "Node")]
    [InlineData("query { product { __typename } }", "Product")]
    [Trait("Category", "Supplemental")]
    public async Task Gateway_Should_ReportTypeNameResult_When_InterfaceObjectIsNotResolvable(
        string query,
        string snapshotName)
    {
        // arrange
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<NonResolvableInterfaceObjectTests>(
                capture,
                (ASubgraph.Name, ASubgraph.BuildAsync),
                (BSubgraph.Name, BSubgraph.BuildAsync));

        // act
        await using var result = await gateway.Executor.ExecuteAsync(
            query,
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create(snapshotName)
            .Add(result, "Gateway Result")
            .Add(capture.Requests, "Source Requests")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task Gateway_Should_NotCallSource_When_InterfaceObjectIsNotResolvable()
    {
        var testCase = AuditFixture.GetOfficialV2Case("non-resolvable-interface-object/001");
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<NonResolvableInterfaceObjectTests>(
                capture,
                (ASubgraph.Name, ASubgraph.BuildAsync),
                (BSubgraph.Name, BSubgraph.BuildAsync));

        var result = await gateway.Executor.ExecuteAsync(
            testCase.Query,
            TestContext.Current.CancellationToken);
        var json = result.ToJson(withIndentations: false);

        AuditAssertions.Assert(json, expectedDataJson: "null", expectsErrors: true);
        Assert.Empty(capture.Requests);
    }
}
