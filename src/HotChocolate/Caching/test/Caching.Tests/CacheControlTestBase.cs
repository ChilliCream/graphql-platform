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
    protected Mock<TCache> GetMock<TCache>()
        where TCache : QueryCache
    {
        return new Mock<TCache>() { CallBase = true };
    }

    protected Mock<QueryCache> GetMock()
    {
        return GetMock<QueryCache>();
    }

    protected void AssertNoWritesToCache<TCache>(Mock<TCache> cacheMock)
        where TCache : QueryCache
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Never());
    }

    protected void AssertOneWriteToCache<TCache>(Mock<TCache> cacheMock)
        where TCache : QueryCache
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    protected void AssertOneWriteToCache<TCache>(Mock<TCache> cacheMock,
        Expression<Func<ICacheControlResult, bool>>? isValidResult)
        where TCache : QueryCache
    {
        cacheMock.Verify(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.Is(isValidResult),
            It.IsAny<ICacheControlOptions>()),
        Times.Once());
    }

    protected (IRequestExecutorBuilder, Mock<QueryCache>)
        GetExecutorBuilderAndCache()
    {
        return GetExecutorBuilderAndCache<QueryCache>();
    }

    protected (IRequestExecutorBuilder, Mock<TCache>)
        GetExecutorBuilderAndCache<TCache>()
        where TCache : QueryCache
    {
        var cache = GetMock<TCache>();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .UseField(_ => _ => default);

        return (builder, cache);
    }

    protected async Task ExecuteRequestAsync(
        IRequestExecutorBuilder builder, string query)
    {
        await ExecuteRequestAsync(builder, null, query);
    }

    protected async Task ExecuteRequestAsync(
        IRequestExecutorBuilder builder, string? schemaName, string query)
    {
        var result = await builder.ExecuteRequestAsync(query, schemaName!);
        var queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
    }
}
