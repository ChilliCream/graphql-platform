using System.Text.Json;
using HotChocolate.CostAnalysis.Utilities;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An unannotated composite list falls through the list-size priority chain to
/// <see cref="CostOptions.DefaultListSize"/>, which defaults to
/// <see cref="double.PositiveInfinity"/> (R-DEFAULT-LIST-SIZE). Infinite values in
/// extensions/error payloads are emitted as the JSON string <c>"Infinity"</c>.
/// </summary>
public sealed class InfinityReportingTests
{
    private const string Schema =
        """
        type Query {
            items: [Item]
        }

        type Item {
            value: Int
        }
        """;

    private const string Operation = "{ items { value } }";

    [Fact]
    public async Task Infinity_Should_ReportAsJsonString_When_ModeIsReport()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.EnforceCostLimits = false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(response.ToJson(withIndentations: false));
        var typeCost = document.RootElement
            .GetProperty("extensions")
            .GetProperty("operationCost")
            .GetProperty("typeCost");

        // assert
        Assert.Equal(JsonValueKind.String, typeCost.ValueKind);
        Assert.Equal("Infinity", typeCost.GetString());
    }

    [Fact]
    public async Task Infinity_Should_RejectWithInfinityTypeCost_When_EnforcementIsOn()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;
        var extensions = result.Errors[0].Extensions!;

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, result.Errors[0].Code);
        Assert.Equal("Infinity", operationCost["typeCost"]);
        Assert.Equal("Infinity", extensions["typeCost"]);
    }

    [Fact]
    public async Task Infinity_Should_ExecuteSuccessfully_When_DefaultListSizeIsOne()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.DefaultListSize = 1)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;

        // assert
        Assert.Empty(result.Errors);
        Assert.Equal(2d, Convert.ToDouble(operationCost["typeCost"]));
    }

    [Fact]
    public async Task Infinity_Should_ReportResponseSizeAsString_When_ResponseSizeIsRejected()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = double.PositiveInfinity;
                    o.MaxTypeCost = double.PositiveInfinity;
                    o.MaxResponseSize = 1;
                })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions["operationCost"]!;
        var errorExtensions = result.Errors[0].Extensions!;

        // assert
        Assert.Equal("Infinity", operationCost["maxResponseSize"]);
        Assert.Equal("Infinity", errorExtensions["maxResponseSize"]);
    }

    [Fact]
    public void AddCostMetrics_Should_UseHC0048_When_ResultStateIsInvalid()
    {
        // arrange
        IExecutionResult? result = null;

        // act
        var response = result.AddCostMetrics(new CostMetrics()).ExpectOperationResult();

        // assert
        Assert.Equal(ErrorCodes.Execution.CostStateInvalid, response.Errors[0].Code);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "items", _ => Array.Empty<object>())
            .AddResolver("Item", "value", _ => 0)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
