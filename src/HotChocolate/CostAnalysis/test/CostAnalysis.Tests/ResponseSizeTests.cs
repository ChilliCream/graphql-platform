using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies response-size enforcement and reporting through
/// <see cref="CostOptions.MaxResponseSize"/>.
/// </summary>
public sealed class ResponseSizeTests
{
    private const string Schema =
        """
        type Query {
            items(limit: Int!): [Item] @listSize(slicingArguments: ["limit"])
        }

        type Item {
            value: Int
        }
        """;

    private const string Operation = "{ items(limit: 500) { value } }";

    [Fact]
    public async Task ResponseSize_Should_RejectWithHC0047_When_ResponseSizeExceedsMaxResponseSize()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxResponseSize = 100)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var error = response.ExpectOperationResult().Errors[0];
        var extensions = error.Extensions!;

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(501d, Convert.ToDouble(extensions["maxResponseSize"]));
        Assert.Equal(100d, Convert.ToDouble(extensions["maxAllowedResponseSize"]));
    }

    [Fact]
    public async Task ResponseSize_Should_FailFast_When_RequestOverride_But_SchemaDoesNotEnableAnalysis()
    {
        // arrange
        var executed = false;
        var requestExecutor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "items", _ => { executed = true; return Array.Empty<object>(); })
            .AddResolver("Item", "value", _ => 0)
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var requestOptions = requestExecutor.GetCostOptions();

        // The schema has response-size analysis disabled.
        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .SetCostOptions(requestOptions with { MaxResponseSize = 1_000 })
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var error = result.Errors![0];

        // assert
        Assert.Equal(ErrorCodes.Execution.ResponseSizeAnalysisNotEnabled, error.Code);
        Assert.Equal(
            "The request cost options set MaxResponseSize, but the schema does not "
            + "enable the response-size analysis.",
            error.Message);
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.ValidationErrors, out var flag));
        Assert.Equal(true, flag);
        Assert.False(executed);
    }

    [Fact]
    public async Task ResponseSize_Should_Pass_When_RequestOverrideLoosensSchemaLimit()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxResponseSize = 100)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var requestOptions = requestExecutor.GetCostOptions();

        // The response size of 500 lies between the schema limit and the higher request limit.
        var request = OperationRequestBuilder.New()
            .SetDocument("{ items(limit: 499) { value } }")
            .SetCostOptions(requestOptions with { MaxResponseSize = 1_000 })
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(response.ExpectOperationResult().Errors is null or { Count: 0 });
    }

    [Fact]
    public async Task ResponseSize_Should_RejectWithHC0047_When_RequestOverrideTightensSchemaLimit()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxResponseSize = 1_000)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var requestOptions = requestExecutor.GetCostOptions();

        // The response size of 500 lies between the lower request limit and the schema limit.
        var request = OperationRequestBuilder.New()
            .SetDocument("{ items(limit: 499) { value } }")
            .SetCostOptions(requestOptions with { MaxResponseSize = 100 })
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var error = response.ExpectOperationResult().Errors[0];
        var extensions = error.Extensions!;

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(500d, Convert.ToDouble(extensions["maxResponseSize"]));
        Assert.Equal(100d, Convert.ToDouble(extensions["maxAllowedResponseSize"]));
    }

    [Fact]
    public async Task ResponseSize_Should_ReportMaxResponseSize_When_ModeIsReportAndWithinLimit()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxResponseSize = 1_000)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var operationCost = (IReadOnlyDictionary<string, object?>)response.ExpectOperationResult()
            .Extensions["operationCost"]!;

        // assert
        Assert.Equal(501d, Convert.ToDouble(operationCost["maxResponseSize"]));
    }

    [Fact]
    public async Task ResponseSize_Should_OmitMaxResponseSizeKey_When_MaxResponseSizeIsNull()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var operationCost = (IReadOnlyDictionary<string, object?>)response.ExpectOperationResult()
            .Extensions["operationCost"]!;

        // assert
        Assert.Equal(["fieldCost", "typeCost"], operationCost.Keys);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "items", _ => Array.Empty<object>())
            .AddResolver("Item", "value", _ => 0)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
