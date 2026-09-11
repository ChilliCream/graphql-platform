using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// <c>GraphQL-Cost: validate</c> reports cost without executing an operation. An empty
/// variable payload uses the static bound only while an active cost analyzer will consume it.
/// </summary>
public sealed class ValidateModeTests
{
    private const string Operation = "query($first: Int) { books(first: $first) { nodes { title } } }";
    private const string RequiredVariableOperation =
        "query($first: Int!) { books(first: $first) { nodes { title } } }";

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public async Task Validate_Should_ReportStaticBound_When_VariablePayloadIsEmpty(string? variableValues)
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var requestBuilder = OperationRequestBuilder.New().SetDocument(Operation).ValidateCost();

        if (variableValues is not null)
        {
            requestBuilder.SetVariableValues(variableValues);
        }

        // act
        var response = await requestExecutor.ExecuteAsync(
            requestBuilder.Build(),
            TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;

        // assert
        Assert.True(result.Data is null or { IsValueNull: true });
        Assert.Equal(200, result.ContextData[ExecutionContextData.HttpStatusCode]);
        Assert.Equal(52d, Convert.ToDouble(operationCost["typeCost"]));
        Assert.Equal(11d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    [Fact]
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
        var result = response.ExpectOperationResult();

        // assert
        Assert.True(result.Data is null or { IsValueNull: true });
        Assert.Equal(200, result.ContextData[ExecutionContextData.HttpStatusCode]);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
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

        // assert
        Assert.Equal(200, result.ContextData[ExecutionContextData.HttpStatusCode]);
        await snapshot
            .AddResult(result, "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Validate_Should_CoerceVariables_When_SchemaAnalyzerIsSkipped()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.SkipAnalyzer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Variable `first` is required.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 7
                    }
                  ],
                  "extensions": {
                    "code": "HC0018",
                    "variable": "first"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Validate_Should_CoerceVariables_When_RequestAnalyzerIsSkipped()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .SetCostOptions(
                new RequestCostOptions(
                    maxFieldCost: 1_000,
                    maxTypeCost: 1_000,
                    enforceCostLimits: false,
                    skipAnalyzer: true,
                    maxResponseSize: null))
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();

        // assert
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, result.Errors[0].Code);
    }

    [Fact]
    public async Task Validate_Should_CoerceVariables_When_AnalyzerIsAbsent()
    {
        // arrange
        var requestExecutor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PagingTests.Query>()
            .AddFiltering()
            .AddSorting()
            .UseDefaultPipeline()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();

        // assert
        Assert.Equal(ErrorCodes.Execution.NonNullViolation, result.Errors[0].Code);
    }

    [Fact]
    public async Task Validate_Should_ReportEvaluatedCost_When_VariablePayloadIsAnArray()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .SetVariableValues("""[{ "first": 3 }]""")
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;

        // assert
        Assert.Equal(5d, Convert.ToDouble(operationCost["typeCost"]));
        Assert.Equal(11d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    [Fact]
    public void SetVariableValues_Should_RejectScalarPayload_When_ValidateModeIsRequested()
    {
        // arrange
        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .ValidateCost();

        // act
        void SetScalarPayload() => requestBuilder.SetVariableValues("1");

        // assert
        Assert.Throws<ArgumentException>(SetScalarPayload);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<PagingTests.Query>()
            .AddFiltering()
            .AddSorting();
}
