using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests exact and overestimated request costs after the compilation case budget is exhausted.
/// </summary>
public sealed class CaseBudgetExceededBehaviorTests
{
    private const string Schema =
        """
        type Query {
            a: Boolean! @cost(weight: "1")
            b: Boolean! @cost(weight: "2")
            c: Boolean! @cost(weight: "4")
        }
        """;

    private const string Operation =
        """
        query($x: Boolean!, $y: Boolean!, $z: Boolean!) {
            a @include(if: $x)
            b @include(if: $y)
            c @include(if: $z)
        }
        """;

    [Fact]
    public async Task CaseBudgetExceededBehavior_Should_PriceExactly_When_DefaultBehaviorIsUsed()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.CaseBudget = 0)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .SetVariableValues(new Dictionary<string, object?> { ["x"] = true, ["y"] = false, ["z"] = true })
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions!["operationCost"]!;

        // assert
        Assert.Equal(5d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    [Fact]
    public async Task CaseBudgetExceededBehavior_Should_PriceByEnvelope_When_OverestimateIsConfigured()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o =>
            {
                o.CaseBudget = 0;
                o.CaseBudgetExceededBehavior = CaseBudgetExceededBehavior.Overestimate;
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var request = OperationRequestBuilder.New()
            .SetDocument(Operation)
            .SetVariableValues(new Dictionary<string, object?> { ["x"] = true, ["y"] = false, ["z"] = true })
            .ValidateCost()
            .Build();

        // act
        var result = (await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken))
            .ExpectOperationResult();
        var operationCost = (IReadOnlyDictionary<string, object?>)result.Extensions!["operationCost"]!;

        // assert
        Assert.Equal(7d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "a", _ => true)
            .AddResolver("Query", "b", _ => false)
            .AddResolver("Query", "c", _ => true);
}
