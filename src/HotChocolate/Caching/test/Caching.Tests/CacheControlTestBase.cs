using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Caching.Tests;

public abstract class CacheControlTestBase
{
    protected Mock<DefaultQueryCache> GetMockedCache()
    {
        return new Mock<DefaultQueryCache>() { CallBase = true };
    }

    protected void AssertNoWritesToCache(Mock<DefaultQueryCache> cacheMock)
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Never());
    }

    protected void AssertOneWriteToCache(Mock<DefaultQueryCache> cacheMock)
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    public async Task AssertNoWritesToCacheAsync(string query)
    {
        var (executor, cache) = await GetExecutorForCacheControlSchemaAsync();

        IExecutionResult result = await executor.ExecuteAsync(query);
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        cache.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Never());
    }

    public async Task AssertOneWriteToCacheAsync(string query,
        Expression<Func<ICacheControlResult, bool>>? isValidResult)
    {
        var (executor, cache) = await GetExecutorForCacheControlSchemaAsync();

        IExecutionResult result = await executor.ExecuteAsync(query);
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        cache.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.Is(isValidResult),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    private async Task<(IRequestExecutor, Mock<DefaultQueryCache>)>
        GetExecutorForCacheControlSchemaAsync()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        return (executor, cache);
    }
}
