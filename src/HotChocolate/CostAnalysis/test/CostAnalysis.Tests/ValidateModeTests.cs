using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// <c>GraphQL-Cost: validate</c> reports cost without executing an operation. It always runs
/// variable coercion first, exactly like <c>execute</c>/<c>report</c>; a required variable that
/// was never supplied fails with the ordinary variable coercion error rather than falling back
/// to a static bound.
/// </summary>
public sealed class ValidateModeTests
{
    private const string Operation = "query($first: Int) { books(first: $first) { nodes { title } } }";
    private const string RequiredVariableOperation =
        "query($first: Int!) { books(first: $first) { nodes { title } } }";

    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    public async Task Validate_Should_ReturnCoercionError_When_RequiredVariablesAreMissing(
        string? variableValues)
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(RequiredVariableOperation)
            .ValidateCost();

        if (variableValues is not null)
        {
            requestBuilder.SetVariableValues(variableValues);
        }

        // act
        var response = await requestExecutor.ExecuteAsync(
            requestBuilder.Build(),
            TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();

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
    public async Task Validate_Should_ReportEvaluatedCost_When_OptionalVariableIsOmitted()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(Operation)
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
    public async Task Validate_Should_ReturnStateInvalid_When_VariableBatchIsEmpty()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .SetVariableValues("[]")
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();

        // assert
        // A non-warmup request that reaches the analyzer with zero coerced variable
        // sets (an explicit empty variable batch) must never fall back to the static
        // bound; it fails with a state-invalid error instead.
        Assert.Equal(ErrorCodes.Execution.CostStateInvalid, result.Errors[0].Code);
        Assert.Equal(
            "The cost analysis requires at least one coerced variable value set.",
            result.Errors[0].Message);
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
