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
        Mock<DefaultQueryCache> cache = GetMockedCache();

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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task SkipQueryCaching()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

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
        Mock<DefaultQueryCache> cache = GetMockedCache();

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

    //[Fact]
    //public async Task ReadFromAllCaches()
    //{
    //    Mock<DefaultQueryCache> cache1 = GetMockedCache();
    //    Mock<DefaultQueryCache> cache2 = GetMockedCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryType(d => d.Name("Query")
    //            .Field("field").Type<StringType>().CacheControl(100))
    //        .UseField(_ => _ => default)
    //        .AddQueryCache(_ => cache1.Object)
    //        .AddQueryCache(_ => cache2.Object)
    //        .UseQueryCachePipeline()
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executor.ExecuteAsync("{ field }");
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);

    //    AssertOneReadFromCache(cache1);
    //    AssertOneReadFromCache(cache2);
    //}

    //[Fact]
    //public async Task DoNotReadFromAllCachesIfOneResolvesTheCachedResult()
    //{
    //    // cache1 should return result
    //    Mock<DefaultQueryCache> cache1 = GetMockedCache();
    //    Mock<DefaultQueryCache> cache2 = GetMockedCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryType(d => d.Name("Query")
    //            .Field("field").Type<StringType>().CacheControl(100))
    //        .UseField(_ => _ => default)
    //        .AddQueryCache(_ => cache1.Object)
    //        .AddQueryCache(_ => cache2.Object)
    //        .UseQueryCachePipeline()
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executor.ExecuteAsync("{ field }");
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);

    //    AssertOneReadFromCache(cache1);
    //    AssertNoReadFromCache(cache2);
    //}

    //[Fact]
    //public async Task SkipReadIfShouldReadResultFromCacheReturnsFalse()
    //{
    //    // ShouldRead should return false
    //    Mock<DefaultQueryCache> cache = GetMockedCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryType(d => d.Name("Query")
    //            .Field("field").Type<StringType>().CacheControl(100))
    //        .UseField(_ => _ => default)
    //        .AddQueryCache(_ => cache.Object)
    //        .UseQueryCachePipeline()
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executor.ExecuteAsync("{ field }");
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);

    //    AssertNoReadFromCache(cache);
    //}

    //[Fact]
    //public async Task IgnoreExceptionInShouldReadFromCache()
    //{
    //    // ShouldRead should throw
    //    Mock<DefaultQueryCache> cache = GetMockedCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryType(d => d.Name("Query")
    //            .Field("field").Type<StringType>().CacheControl(100))
    //        .UseField(_ => _ => default)
    //        .AddQueryCache(_ => cache.Object)
    //        .UseQueryCachePipeline()
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executor.ExecuteAsync("{ field }");
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);

    //    AssertNotReadsFromCache(cache);
    //}

    //[Fact]
    //public async Task IgnoreExceptionInReadCachedQueryResult()
    //{
    //    // Read should throw
    //    Mock<DefaultQueryCache> cache = GetMockedCache();

    //    IRequestExecutorBuilder builder = new ServiceCollection()
    //        .AddGraphQLServer()
    //        .AddQueryType(d => d.Name("Query")
    //            .Field("field").Type<StringType>().CacheControl(100))
    //        .UseField(_ => _ => default)
    //        .AddQueryCache(_ => cache.Object)
    //        .UseQueryCachePipeline()
    //        .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

    //    IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

    //    IExecutionResult result = await executor.ExecuteAsync("{ field }");
    //    IQueryResult queryResult = result.ExpectQueryResult();

    //    Assert.Null(queryResult.Errors);

    //    AssertNotReadsFromCache(cache);
    //}

    [Fact]
    public async Task WriteToAllCaches()
    {
        Mock<DefaultQueryCache> cache1 = GetMockedCache();
        Mock<DefaultQueryCache> cache2 = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1.Object)
            .AddQueryCache(_ => cache2.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertOneWriteToCache(cache1);
        AssertOneWriteToCache(cache2);
    }

    [Fact]
    public async Task DoNotWriteToCacheIfNoMaxAgeCouldBeComputed()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ __typename }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task SkipWriteIfShouldCacheResultReturnsFalse()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task IgnoreExceptionInShouldCacheResult()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task IgnoreExceptionInCacheQueryResult()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertOneWriteToCache(cache);
    }
}
