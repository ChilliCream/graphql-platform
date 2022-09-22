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
        var cache = GetMock();

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var query = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .SkipQueryCaching()
            .Create();

        var result = await executor.ExecuteAsync(query);
        var queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task QueryCachingIsOnlyAppliedToTargetSchema()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
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

        var executorWithCache = await builder
            .BuildRequestExecutorAsync("withcache");
        var executorWithCacheResult = await executorWithCache
            .ExecuteAsync("{ a: field }");
        var executorWithCacheQueryResult = executorWithCacheResult
            .ExpectQueryResult();

        var executorWithoutCache = await builder
            .BuildRequestExecutorAsync("withoutcache");
        var executorWithoutCacheResult = await executorWithoutCache
            .ExecuteAsync("{ b: field }");
        var executorWithoutCacheQueryResult = executorWithoutCacheResult
            .ExpectQueryResult();

        Assert.Null(executorWithCacheQueryResult.Errors);
        Assert.Null(executorWithoutCacheQueryResult.Errors);

        AssertOneWriteToCache(cache);
    }

    [Fact(Skip = "Until reading is enabled")]
    public async Task ReadFromAllCaches()
    {
        var cache1 = GetMock();
        var cache2 = GetMock();

        var builder = new ServiceCollection()
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
        var cache1 = GetMock();
        var cache2 = GetMock();

        var cachedResult = QueryResultBuilder.New()
            .SetData(new Dictionary<string, object?>
            {
                { "field", "value" }
            })
            .Create();

        cache1.Setup(x => x.TryReadCachedQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlOptions>()))
            .Returns(Task.FromResult<IQueryResult?>(cachedResult));

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.ShouldReadResultFromCache(
                It.IsAny<IRequestContext>()))
            .Returns(false);

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.ShouldReadResultFromCache(
                It.IsAny<IRequestContext>()))
            .Throws(new System.Exception());

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.TryReadCachedQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlOptions>()))
            .Throws(new System.Exception());

        var builder = new ServiceCollection()
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
        var cache1 = GetMock();
        var cache2 = GetMock();

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.ShouldCacheResult(
            It.IsAny<IRequestContext>()))
            .Returns(false);

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.ShouldCacheResult(
            It.IsAny<IRequestContext>()))
            .Throws(new System.Exception());

        var builder = new ServiceCollection()
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
        var cache = GetMock();

        cache.Setup(x => x.CacheQueryResultAsync(
            It.IsAny<IRequestContext>(),
            It.IsAny<ICacheControlResult>(),
            It.IsAny<ICacheControlOptions>()))
            .Throws(new System.Exception());

        var builder = new ServiceCollection()
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
