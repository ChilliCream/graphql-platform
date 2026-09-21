using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests that each request obtains its normalized document once across coercion and cost analysis.
/// </summary>
public sealed class CostAnalyzerNormalizerCallCountTests
{
    private const string Schema =
        """
        type Query {
            foo: String
            bar: String
        }
        """;

    private const string OperationA =
        """
        query NormalizerCallCountA {
            foo
        }
        """;

    private const string OperationB =
        """
        query NormalizerCallCountB {
            bar
        }
        """;

    [Fact]
    public async Task Every_Request_Normalizes_Exactly_Once_Regardless_Of_Cache_State()
    {
        // arrange
        var normalizeCallCount = 0;

        var requestExecutor = await CreateRequestExecutorBuilder(
                () => Interlocked.Increment(ref normalizeCallCount))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var missResult = await requestExecutor.ExecuteAsync(OperationA, TestContext.Current.CancellationToken);
        var countAfterMiss = Volatile.Read(ref normalizeCallCount);

        var hitResult = await requestExecutor.ExecuteAsync(OperationA, TestContext.Current.CancellationToken);
        var countAfterHit = Volatile.Read(ref normalizeCallCount);

        // assert
        Assert.Empty(missResult.ExpectOperationResult().Errors);
        Assert.Empty(hitResult.ExpectOperationResult().Errors);
        Assert.Equal(1, countAfterMiss);
        Assert.Equal(2, countAfterHit);
    }

    [Fact]
    public async Task Plan_Cache_Miss_Does_Not_Normalize_A_Second_Time_Within_The_Request()
    {
        // arrange
        // A capacity of one evicts the first cost plan while the operation cache retains both operations.
        var normalizeCallCount = 0;

        var requestExecutor = await CreateRequestExecutorBuilder(
                () => Interlocked.Increment(ref normalizeCallCount))
            .ModifyCostOptions(o => o.CostPlanCacheSize = 1)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var operationCache = requestExecutor.Schema.Services.GetRequiredService<IPreparedOperationCache>();
        var planCache = requestExecutor.Schema.Services.GetRequiredService<CostPlanCache>();

        // act
        await requestExecutor.ExecuteAsync(OperationA, TestContext.Current.CancellationToken);
        var countAfterFirstMiss = Volatile.Read(ref normalizeCallCount);

        await requestExecutor.ExecuteAsync(OperationB, TestContext.Current.CancellationToken);
        var countAfterEviction = Volatile.Read(ref normalizeCallCount);

        await requestExecutor.ExecuteAsync(OperationA, TestContext.Current.CancellationToken);
        var countAfterPlanMiss = Volatile.Read(ref normalizeCallCount);

        // assert
        Assert.Equal(2, operationCache.Count);
        Assert.Equal(1, planCache.Count);
        Assert.Equal(1, countAfterFirstMiss);
        Assert.Equal(2, countAfterEviction);
        Assert.Equal(3, countAfterPlanMiss);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder(Action onNormalize)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "foo", _ => "foo")
            .AddResolver("Query", "bar", _ => "bar")
            .ConfigureSchemaServices(
                services => services.AddSingleton<IOperationDocumentNormalizer>(
                    _ => new CountingNormalizer(onNormalize)));

    // Both operations have no fragments or static conditions, so their parsed documents are already normalized.
    private sealed class CountingNormalizer(Action onNormalize) : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return context.OperationDocumentInfo.Document!;
        }
    }
}
