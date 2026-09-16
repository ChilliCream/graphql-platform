using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// End-to-end coverage for <see cref="CostOptions.CaseBudgetExceededBehavior"/>: once an
/// operation exceeds the case budget, the default behavior prices it exactly per request,
/// while <see cref="CostAnalysis.CaseBudgetExceededBehavior.Overestimate"/> prices it by the
/// compiled envelope instead.
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
        // arrange: the case budget affords no exact split, so the default (EvaluatePerRequest)
        // mode re-derives the exact cost from the operation's condition tree
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

        // assert: only the included fields (a and c) are billed, the excluded field (b) is not
        Assert.Equal(5d, Convert.ToDouble(operationCost["fieldCost"]));
    }

    [Fact]
    public async Task CaseBudgetExceededBehavior_Should_PriceByEnvelope_When_OverestimateIsConfigured()
    {
        // arrange: the same operation and variables, but Overestimate bakes a conservative
        // envelope for the whole operation into the compiled plan instead
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

        // assert: every field is billed as if included, regardless of the excluded one
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
