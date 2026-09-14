using HotChocolate.Collections.Immutable;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A <see cref="VariableBatchRequest"/> sums field and type costs across every coerced variable set for request-level enforcement.
/// Successful and validate batches keep per-result reporting; a limit violation rejects the whole request (R-BATCH-REPORTING, R-BATCH-SUM).
/// </summary>
public sealed class BatchReportingTests
{
    private int _executionCount;

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

    [Fact]
    public async Task Batch_Should_ReportPerItemOperationCost_When_ModeIsReport()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = 4_000;
                    o.MaxTypeCost = 2_000;
                })
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

        // assert
        Assert.Equal(2, _executionCount);
        await snapshot
            .Add(batch.Results.Count, "ResultCount")
            .AddResult((OperationResult)batch.Results[0], "FirstSet")
            .AddResult((OperationResult)batch.Results[1], "SecondSet")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Batch_Should_ReportPerItemOperationCostWithoutExecution_When_ModeIsValidate()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = 1;
                    o.MaxTypeCost = 1;
                })
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
                .ValidateCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var batch = response.ExpectOperationResultBatch();

        // assert
        Assert.Equal(0, _executionCount);
        await snapshot
            .Add(batch.Results.Count, "ResultCount")
            .AddResult((OperationResult)batch.Results[0], "FirstSet")
            .AddResult((OperationResult)batch.Results[1], "SecondSet")
            .Add(
                batch.Results
                    .Cast<OperationResult>()
                    .Select(t => t.ContextData[ExecutionContextData.HttpStatusCode])
                    .ToArray(),
                "HttpStatusCodes")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Batch_Should_RejectWholeRequest_When_SummedCostExceedsLimit()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = 4_000;
                    o.MaxTypeCost = 150;
                })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["n"] = 100 },
                        new Dictionary<string, object?> { ["n"] = 100 }
                    })
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();

        // assert
        Assert.Equal(0, _executionCount);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Batch_Should_RejectWholeRequest_When_OneSetExceedsLimit()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = 4_000;
                    o.MaxTypeCost = 500;
                })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["n"] = 1000 },
                        new Dictionary<string, object?> { ["n"] = 1 }
                    })
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();

        // assert
        Assert.Equal(0, _executionCount);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Batch_Should_RejectWholeRequestAtFirstResponseSizeViolation_When_MultipleSetsExceedLimit()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = 1_000;
                    o.MaxTypeCost = 1_000;
                    o.MaxResponseSize = 5;
                })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues(
                    new List<IReadOnlyDictionary<string, object?>>
                    {
                        new Dictionary<string, object?> { ["n"] = 10 },
                        new Dictionary<string, object?> { ["n"] = 20 }
                    })
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();

        // assert
        Assert.Equal(0, _executionCount);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ResponseStream_Should_ReportOperationCostOnFirstResult()
    {
        // arrange
        var stream = new ResponseStream(CreateResults);
        var metrics = new CostMetrics { FieldCost = 2, TypeCost = 3 };
        var results = new List<string>();

        // act
        stream.AddCostMetrics(metrics);
        await foreach (var result in stream.ReadResultsAsync())
        {
            results.Add(result.ToJson(withIndentations: false));
        }

        // assert
        results.MatchInlineSnapshots(
            [
                """{"extensions":{"item":1,"operationCost":{"fieldCost":2,"typeCost":3}}}""",
                """{"extensions":{"item":2}}"""
            ]);
    }

    private static async IAsyncEnumerable<OperationResult> CreateResults()
    {
        await Task.Yield();
        yield return new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("item", 1));
        yield return new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("item", 2));
    }

    private IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver(
                "Query",
                "items",
                _ =>
                {
                    Interlocked.Increment(ref _executionCount);
                    return Array.Empty<object>();
                })
            .AddResolver("Item", "value", _ => 0)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
