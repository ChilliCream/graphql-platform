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

    [Fact(Skip = "enabled by hc-reporting")]
    public async Task Infinity_Should_ReportAsJsonString_When_ModeIsReport()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.EnforceCostLimits = false)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var operationCost = (IReadOnlyDictionary<string, object?>)response.ExpectOperationResult()
            .Extensions["operationCost"]!;

        // assert
        Assert.Equal("Infinity", operationCost["typeCost"]);
    }

    [Fact(Skip = "enabled by hc-reporting")]
    public async Task Infinity_Should_RejectWithInfinityTypeCost_When_EnforcementIsOn()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var result = response.ExpectOperationResult();
        var extensions = result.Errors[0].Extensions!;

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, result.Errors[0].Code);
        Assert.Equal("Infinity", extensions["typeCost"]);
    }

    [Fact(Skip = "enabled by hc-reporting")]
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

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
