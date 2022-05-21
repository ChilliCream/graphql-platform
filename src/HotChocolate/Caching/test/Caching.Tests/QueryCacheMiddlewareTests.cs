using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class QueryCacheMiddlewareTests : CacheControlTestBase
{
    [Fact]
    public async Task QueryCachingDisabled()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o =>
            {
                o.Enable = false;
            });

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task SkipQueryCaching()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IReadOnlyQueryRequest query = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .SkipQueryCaching()
            .Create();

        IExecutionResult result = await executor.ExecuteAsync(query);
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task QueryCachingIsOnlyAppliedToTargetSchema()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer("withcache")
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
            .AddGraphQLServer("withoutcache")
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>())
            .UseField(_ => _ => default);

        IRequestExecutor executorWithCache = await builder
            .BuildRequestExecutorAsync("withcache");
        IExecutionResult executorWithCacheResult = await executorWithCache
            .ExecuteAsync("{ a: field }");
        IQueryResult executorWithCacheQueryResult = executorWithCacheResult
            .ExpectQueryResult();

        IRequestExecutor executorWithoutCache = await builder
            .BuildRequestExecutorAsync("withoutcache");
        IExecutionResult executorWithoutCacheResult = await executorWithoutCache
            .ExecuteAsync("{ b: field }");
        IQueryResult executorWithoutCacheQueryResult = executorWithoutCacheResult
            .ExpectQueryResult();

        Assert.Null(executorWithCacheQueryResult.Errors);
        Assert.Null(executorWithoutCacheQueryResult.Errors);

        AssertOneWriteToCache(cache);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task ReadFromAllCaches()
    {
        Mock<DefaultQueryCache> cache1 = GetMock();
        Mock<DefaultQueryCache> cache2 = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1.Object)
            .AddQueryCache(_ => cache2.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneReadFromCache(cache1);
        AssertOneReadFromCache(cache2);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task DoNotReadFromAllCachesIfOneResolvesTheCachedResult()
    {
        Mock<DefaultQueryCache> cache1 = GetMock();
        Mock<DefaultQueryCache> cache2 = GetMock();

        IQueryResult cachedResult = QueryResultBuilder.New()
            .SetData(new Dictionary<string, object?>
            {
                { "field", "value" }
            })
            .Create();

        cache1.Setup(x => x.TryReadCachedQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlOptions>()))
            .Returns(Task.FromResult<IQueryResult?>(cachedResult));

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1.Object)
            .AddQueryCache(_ => cache2.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneReadFromCache(cache1);
        AssertNoReadsFromCache(cache2);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task SkipReadIfShouldReadResultFromCacheReturnsFalse()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.ShouldReadResultFromCache(
                It.IsAny<IRequestContext>()))
            .Returns(false);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoReadsFromCache(cache);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task IgnoreExceptionInShouldReadFromCache()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.ShouldReadResultFromCache(
                It.IsAny<IRequestContext>()))
            .Throws(new System.Exception());

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoReadsFromCache(cache);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task IgnoreExceptionInTryReadCachedQueryResultAsync()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.TryReadCachedQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlOptions>()))
            .Throws(new System.Exception());

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneReadFromCache(cache);
    }

    [Fact]
    public async Task WriteToAllCaches()
    {
        Mock<DefaultQueryCache> cache1 = GetMock();
        Mock<DefaultQueryCache> cache2 = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1.Object)
            .AddQueryCache(_ => cache2.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneWriteToCache(cache1);
        AssertOneWriteToCache(cache2);
    }

    [Fact]
    public async Task DoNotWriteToCacheIfNoMaxAgeCouldBeComputed()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ __typename }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task SkipWriteIfShouldCacheResultReturnsFalse()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.ShouldCacheResult(
            It.IsAny<IRequestContext>()))
            .Returns(false);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task IgnoreExceptionInShouldCacheResult()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.ShouldCacheResult(
            It.IsAny<IRequestContext>()))
            .Throws(new System.Exception());

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task IgnoreExceptionInCacheQueryResult()
    {
        Mock<DefaultQueryCache> cache = GetMock();

        cache.Setup(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()))
            .Throws(new System.Exception());

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneWriteToCache(cache);
    }
}
