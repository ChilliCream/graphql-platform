using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies the per-request <c>ModifyCostOptions</c> path on <see cref="OperationRequestBuilder"/>.
/// </summary>
public sealed class ModifyCostOptionsTests
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
    public async Task ModifyCostOptions_Should_RaiseTypeCostLimit_When_ModifierOverridesUpward()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 100)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .ModifyCostOptions(o => o.MaxTypeCost = 1_000)
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(response.ExpectOperationResult().Errors is null or { Count: 0 });
    }

    [Fact]
    public async Task ModifyCostOptions_Should_LowerTypeCostLimit_When_ModifierOverridesDownward()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 10_000)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .ModifyCostOptions(o => o.MaxTypeCost = 100)
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var error = response.ExpectOperationResult().Errors[0];

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(501d, Convert.ToDouble(error.Extensions!["typeCost"]));
        Assert.Equal(100d, Convert.ToDouble(error.Extensions!["maxTypeCost"]));
    }

    [Fact]
    public async Task ModifyCostOptions_Should_InheritSchemaValue_When_ModifierLeavesItUnset()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 100)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The modifier only touches MaxFieldCost, so MaxTypeCost keeps inheriting the schema value.
        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .ModifyCostOptions(o => o.MaxFieldCost = 999_999)
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var error = response.ExpectOperationResult().Errors[0];

        // assert
        Assert.Equal(ErrorCodes.Execution.CostExceeded, error.Code);
        Assert.Equal(501d, Convert.ToDouble(error.Extensions!["typeCost"]));
        Assert.Equal(100d, Convert.ToDouble(error.Extensions!["maxTypeCost"]));
    }

    [Fact]
    public async Task ModifyCostOptions_Should_BypassAnalyzer_When_ModifierSetsSkipAnalyzer()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 100)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .ModifyCostOptions(o => o.SkipAnalyzer = true)
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(response.ExpectOperationResult().Errors is null or { Count: 0 });
    }

    [Fact]
    public async Task ModifyCostOptions_Should_ApplyModifiersInOrder_When_MultipleModifiersAreAdded()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 10_000)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The second modifier only raises the limit far enough above the operation cost (501) if it
        // sees the first modifier's value (100 + 500 = 600); reversed order would leave it at 100.
        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .ModifyCostOptions(o => o.MaxTypeCost = 100)
            .ModifyCostOptions(o => o.MaxTypeCost += 500)
            .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.True(response.ExpectOperationResult().Errors is null or { Count: 0 });
    }

#pragma warning disable CS0618 // Verifies the obsolete SetCostOptions/RequestCostOptions path.
    [Theory]
    [InlineData(false, ErrorCodes.Execution.CostExceeded)]
    [InlineData(true, null)]
    public async Task SetCostOptions_Should_StillApply_And_YieldToModifiers_When_BothAreSet(
        bool addModifier,
        string? expectedErrorCode)
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder(o => o.MaxTypeCost = 10_000)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var requestBuilder = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .SetCostOptions(new RequestCostOptions(10_000, 100, true, false, (double?)null));

        if (addModifier)
        {
            requestBuilder.ModifyCostOptions(o => o.MaxTypeCost = 10_000);
        }

        // act
        var response = await requestExecutor.ExecuteAsync(
            requestBuilder.Build(),
            TestContext.Current.CancellationToken);
        var errors = response.ExpectOperationResult().Errors;

        // assert
        Assert.Equal(expectedErrorCode, errors is { Count: > 0 } ? errors[0].Code : null);
    }
#pragma warning restore CS0618

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder(Action<CostOptions> configureOptions)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "items", _ => Array.Empty<object>())
            .AddResolver("Item", "value", _ => 0)
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .ModifyCostOptions(configureOptions);
}
