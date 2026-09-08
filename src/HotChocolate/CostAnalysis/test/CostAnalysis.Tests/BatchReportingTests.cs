using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A <see cref="VariableBatchRequest"/> is one request: every coerced variable set is
/// evaluated and reported on its own result, and any over-limit set fails the whole
/// request (R-BATCH-REPORTING, R-HC-BATCH).
/// </summary>
public sealed class BatchReportingTests
{
    private const string Schema =
        """
        type Query {
            items(limit: Int!): [Item] @cost(weight: "1") @listSize(slicingArguments: ["limit"])
        }

        type Item {
            value: Int @cost(weight: "3")
        }
        """;

    private const string Operation = "query($n: Int!) { items(limit: $n) { value } }";

    [Fact(Skip = "enabled by hc-reporting")]
    public async Task Batch_Should_ReportPerItemOperationCost_When_ModeIsReport()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["n"] = 1 },
                        new Dictionary<string, object?> { ["n"] = 1000 }
                    })
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var batch = response.ExpectOperationResultBatch();

        // assert: two variable sets, each carrying its own operationCost.
        await snapshot
            .Add(batch.Results.Count, "ResultCount")
            .AddResult((OperationResult)batch.Results[0], "FirstSet")
            .AddResult((OperationResult)batch.Results[1], "SecondSet")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "enabled by hc-reporting")]
    public async Task Batch_Should_FailWholeRequest_When_OnlyOneSetExceedsMaxTypeCost()
    {
        // arrange
        var snapshot = new Snapshot();

        // typeCost is 1 (root) + n * 1 (Item) per set: 2 for n=1, 1001 for n=1000. A limit
        // of 500 rejects the whole batch even though only the second set exceeds it.
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxTypeCost = 500)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["n"] = 1 },
                        new Dictionary<string, object?> { ["n"] = 1000 }
                    })
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        await snapshot
            .Add(response, "Response")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
