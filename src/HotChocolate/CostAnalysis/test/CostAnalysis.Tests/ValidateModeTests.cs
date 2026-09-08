using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// <c>GraphQL-Cost: validate</c> on the <see cref="PagingTests.Query"/> schema
/// (hc-3-mmh.8 item 4, R-VALIDATE-MODE, R-REPORTING-DETAILS a/b): without variables the
/// request never reaches coercion and reports the static bound; with variables it reports
/// the evaluated cost. Either way there is no execution, no <c>data</c>, and the response
/// carries HTTP status 200 even above the configured limits.
/// </summary>
public sealed class ValidateModeTests
{
    private const string Operation = "query($first: Int) { books(first: $first) { nodes { title } } }";

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Validate_Should_ReportStaticBound_When_NoVariablesAreSupplied()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ValidateCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;

        // assert: the static bound reads the paging field's assumedSize (MaxPageSize, 50)
        // rather than a coerced value, since the request never reaches coercion.
        Assert.True(result.Data is null or { IsValueNull: true });
        Assert.Equal(200, result.ContextData[ExecutionContextData.HttpStatusCode]);
        Assert.Equal(11d, Convert.ToDouble(operationCost["typeCost"]));
        Assert.Equal(52d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Validate_Should_ReportEvaluatedCost_When_VariablesAreSupplied()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues("""{ "first": 3 }""")
                .ValidateCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        await snapshot
            .AddResult(response.ExpectOperationResult(), "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Validate_Should_ReportNumbersAboveLimits_When_CostExceedsMaxTypeCost()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxTypeCost = 1)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
                .SetVariableValues("""{ "first": 3 }""")
                .ValidateCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();

        // assert: validate never enforces, so a cost above the limit is still reported
        // with HTTP status 200 rather than rejected.
        Assert.Equal(200, result.ContextData[ExecutionContextData.HttpStatusCode]);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<PagingTests.Query>()
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
