using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests cost-plan caching for accepted, rejected, and warmup requests.
/// </summary>
public sealed class CostPlanCacheTests
{
    private const string Schema =
        """
        type Query {
            examples(limit: Int! @cost(weight: "2.0")): [Example!]!
                @cost(weight: "3.0") @listSize(slicingArguments: ["limit"])
        }

        type Example @cost(weight: "4.0") {
            exampleField: Boolean!
        }
        """;

    private const string Operation =
        """
        query {
            examples(limit: 10) {
                exampleField
            }
        }
        """;

    private const string BudgetSchema =
        """
        type Query {
            a: Boolean!
            b: Boolean!
            c: Boolean!
        }
        """;

    private const string BudgetOperation =
        """
        query($x: Boolean!, $y: Boolean!, $z: Boolean!) {
            a @include(if: $x)
            b @include(if: $y)
            c @include(if: $z)
        }
        """;

    [Fact]
    public async Task Cache_Should_CompileOnFirstExecution_When_OperationIsNew()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task Cache_Should_EvaluateOnly_When_OperationIsAlreadyCompiled()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var countAfterFirstExecution = cache.Count;
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, countAfterFirstExecution);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task Cache_Should_PopulateEntry_When_OperationIsRejected()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.MaxTypeCost = 1)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        var request = OperationRequestBuilder.New().SetDocument(Operation).ReportCost().Build();

        // act
        var result = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.NotEmpty(result.ExpectOperationResult().Errors);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task Cache_Should_PopulateEntry_When_RequestIsWarmup()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        var request = OperationRequestBuilder.New().SetDocument(Operation).MarkAsWarmupRequest().Build();

        // act
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public async Task Cache_Should_UseConfiguredCapacity_When_ExecutorIsBuilt()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .ModifyCostOptions(o => o.CostPlanCacheSize = 42)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();

        // assert
        Assert.Equal(42, cache.Capacity);
    }

    [Fact]
    public async Task Cache_Should_NotRecompile_When_CaseBudgetTrippingOperationExecutesTwice()
    {
        // arrange
        // Three independent Boolean conditions require seven splits, exceeding the budget of one.
        CostAnalysisResult? firstResult = null;
        CostAnalysisResult? secondResult = null;

        var requestExecutor = await CreateBudgetTrippingRequestExecutorBuilder()
            .UseRequest(
                next => async context =>
                {
                    await next(context);
                    context.TryGetCostAnalysisResult(out var result);

                    if (firstResult is null)
                    {
                        firstResult = result;
                    }
                    else
                    {
                        secondResult = result;
                    }
                },
                key: "CaptureCostAnalysisResult",
                before: WellKnownRequestMiddleware.CostAnalyzerMiddleware)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();
        var request = OperationRequestBuilder.New()
            .SetDocument(BudgetOperation)
            .SetVariableValues(
                new Dictionary<string, object?> { ["x"] = true, ["y"] = false, ["z"] = true })
            .Build();

        // act
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var countAfterFirstExecution = cache.Count;
        await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, countAfterFirstExecution);
        Assert.Equal(1, cache.Count);
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.True(firstResult.Plan.HitCaseBudget);
        Assert.True(secondResult.Plan.HitCaseBudget);
        Assert.Same(firstResult.Plan, secondResult.Plan);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "examples", _ => Array.Empty<object>())
            .AddResolver("Example", "exampleField", _ => false)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);

    private static IRequestExecutorBuilder CreateBudgetTrippingRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(BudgetSchema)
            .AddResolver("Query", "a", _ => true)
            .AddResolver("Query", "b", _ => false)
            .AddResolver("Query", "c", _ => true)
            .ModifyCostOptions(o =>
            {
                o.DefaultResolverCost = null;
                o.CaseBudget = 1;
            });
}
