using HotChocolate.Execution;
using HotChocolate.Fusion.Suites;
using HotChocolate.Fusion.Suites.ChildTypeMismatch.A;
using HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

namespace HotChocolate.Fusion.AdditionalCoverage;

public sealed class ChildTypeMismatchSupplementalTests
{
    [Fact]
    [Trait("Category", "Supplemental")]
    public async Task Operation_Should_AliasOnlyConflictingSourceField_When_SourceTypesDiffer()
    {
        var capture = new SubgraphRequestCapture();
        await using var gateway = await FusionGatewayBuilder
            .ComposeOfficialV2Async<ChildTypeMismatchTests>(
                capture,
                (ASubgraph.Name, ASubgraph.BuildAsync),
                (BSubgraph.Name, BSubgraph.BuildAsync));
        var testCase = AuditFixture.GetOfficialV2Case<ChildTypeMismatchTests>(
            "child-type-mismatch/000");

        var result = await gateway.Executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(testCase.Query).Build(),
            TestContext.Current.CancellationToken);
        AuditAssertions.Assert(result.ToJson(), testCase.ExpectedData, expectsErrors: false);

        await ApolloIntegrationSnapshot.MatchAsync(
            new Snapshot(extension: ".yaml"),
            testCase.Query,
            capture,
            result,
            includeRequestOrder: false);
    }
}
