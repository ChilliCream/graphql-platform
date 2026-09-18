using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Pipeline;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The cost analyzer's hot path never normalizes the document: an operation cache hit whose
/// cost plan is already cached must not call <see cref="IOperationDocumentNormalizer"/> at
/// all, and a cost plan cache miss normalizes exactly once, even when the operation itself
/// was already compiled and cached.
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
    public async Task Operation_Cache_Hit_And_Plan_Cache_Hit_Never_Normalizes()
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

        // assert: the second execution is both an operation cache hit and a cost plan cache
        // hit, so it must not normalize at all.
        Assert.Empty(missResult.ExpectOperationResult().Errors);
        Assert.Empty(hitResult.ExpectOperationResult().Errors);
        Assert.Equal(1, countAfterMiss);
        Assert.Equal(1, countAfterHit);
    }

    [Fact]
    public async Task Operation_Cache_Hit_With_Plan_Cache_Miss_Normalizes_Exactly_Once()
    {
        // arrange: a cost plan cache capacity of one means executing a second, different
        // operation evicts the first operation's plan, while the much larger operation cache
        // keeps both operations compiled and cached.
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

        // assert: both operations remain in the (much larger) operation cache, only one plan
        // fits in the cost plan cache, and re-running operation A normalizes exactly once more
        // because its plan, not its compiled operation, had to be recompiled.
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

    // Neither OperationA nor OperationB contains a fragment, so flattening fragments into the
    // selected operation, the one thing a real normalizer does, is a no-op here: returning the
    // parsed document unchanged is a valid normalized document for these tests.
    private sealed class CountingNormalizer(Action onNormalize) : IOperationDocumentNormalizer
    {
        public DocumentNode NormalizeDocument(RequestContext context)
        {
            onNormalize();
            return context.OperationDocumentInfo.Document!;
        }
    }
}
