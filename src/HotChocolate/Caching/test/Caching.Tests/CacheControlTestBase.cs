using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public abstract class CacheControlTestBase
{
    public Task<CacheControlResult> ValidateResultAsync(
        string query,
        Action<IRequestExecutorBuilder>? configureExecutor = null)
    {
        return ValidateResultAsync(executor => executor.ExecuteAsync(query),
            configureExecutor);
    }

    public async Task<CacheControlResult> ValidateResultAsync(
        Func<IRequestExecutor, Task<IExecutionResult>> executeRequest,
        Action<IRequestExecutorBuilder>? configureExecutor = null)
    {
        var cache = new TestQueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        configureExecutor?.Invoke(builder);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executeRequest(executor);

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Result);

        return cache.Result!;
    }

    private class TestQueryCache : DefaultQueryCache
    {
        public CacheControlResult? Result { get; set; }

        public override Task CacheQueryResultAsync(IRequestContext context, CacheControlResult result,
            ICacheControlOptions options)
        {
            Result = result;

            return Task.CompletedTask;
        }

        public override Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
            ICacheControlOptions options)
        {
            return Task.FromResult<IQueryResult?>(null);
        }
    }
}