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

        capture.ForSubgraph(BSubgraph.Name).Select(static request => request.Body)
            .MatchInlineSnapshot(
                """
                [
                  "{\"query\":\"query Op_56dcc647_1 {\\n  accounts {\\n    __typename\\n    ... on User {\\n      id\\n      name\\n    }\\n    ... on Admin {\\n      fusion__field_1: id\\n      name\\n    }\\n  }\\n}\"}",
                  "{\"query\":\"query($representations: [_Any!]!) {\\n  _entities(representations: $representations) {\\n    ... on User {\\n      name\\n    }\\n  }\\n}\",\"variables\":{\"representations\":[{\"__typename\":\"User\",\"id\":\"u1\"}]}}"
                ]
                """);
    }
}
