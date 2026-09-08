using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// <see cref="CostOptions.MaxResponseSize"/> rides the same cost middleware and the same
/// HC0047 error shape as field/type cost, via the tupled algebra (hc-3-mmh.10 item 4).
/// A paged list of 500 objects with one scalar field each has a maximum response size of
/// 501: 1 for the list field itself, plus 1 per object (its scalar field contributes 0).
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

    [Fact(Skip = "enabled by hc-reporting")]
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

    [Fact(Skip = "enabled by hc-reporting")]
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

    [Fact(Skip = "enabled by hc-reporting")]
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
        Assert.False(operationCost.ContainsKey("maxResponseSize"));
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
