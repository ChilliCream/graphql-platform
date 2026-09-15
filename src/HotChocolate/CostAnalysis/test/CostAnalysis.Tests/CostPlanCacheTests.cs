using HotChocolate.Caching.Memory;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The dedicated <see cref="Cache{TValue}"/> of <see cref="CostPlan"/>, keyed by operation
/// id and reachable from the schema services (hc-3-mmh.8 locked). Rejected and warmup
/// requests populate it exactly like an accepted one; only compilation, never
/// enforcement, decides whether an entry is written.
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

    [Fact]
    public async Task Cache_Should_CompileOnFirstExecution_When_OperationIsNew()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var cache = requestExecutor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
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

        var cache = requestExecutor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
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

        var cache = requestExecutor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
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

        var cache = requestExecutor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
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
        var cache = requestExecutor.Schema.Services.GetRequiredService<Cache<CostPlan>>();

        // assert
        Assert.Equal(42, cache.Capacity);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "examples", _ => Array.Empty<object>())
            .AddResolver("Example", "exampleField", _ => false)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
