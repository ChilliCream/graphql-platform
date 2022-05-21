using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
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

    protected void AssertOneWriteToCache(Mock<DefaultQueryCache> cacheMock,
        Expression<Func<ICacheControlResult, bool>>? isValidResult)
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.Is(isValidResult),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    protected (IRequestExecutorBuilder, Mock<DefaultQueryCache>)
        GetExecutorBuilderAndCache()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);
        
        return (builder, cache);
    }

    protected async Task ExecuteRequestAsync(
        IRequestExecutorBuilder builder, string query)
    {
        IExecutionResult result = await builder.ExecuteRequestAsync(query);
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
    }
}
