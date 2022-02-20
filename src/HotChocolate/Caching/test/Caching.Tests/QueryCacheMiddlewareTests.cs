using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class QueryCacheMiddlewareTests : CacheControlTestBase
{
    [Fact(Skip = "Until options are correctly implemented")]
    public async Task QueryCachingDisabled()
    {
        var cache = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o =>
            {
                o.Enable = false;
            });

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Empty(cache.Reads);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task SkipQueryCaching()
    {
        var cache = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IReadOnlyQueryRequest query = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .SkipQueryCaching()
            .Create();

        IExecutionResult result = await executor.ExecuteAsync(query);

        Assert.Null(result.Errors);
        Assert.Empty(cache.Reads);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task ReadFromAllCaches()
    {
        var cache1 = new QueryCache();
        var cache2 = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1)
            .AddQueryCache(_ => cache2)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache1.Reads);
        Assert.Single(cache2.Reads);
    }

    [Fact]
    public async Task DoNotReadFromAllCachesIfOneResolvesTheCachedResult()
    {
        var cache1 = new QueryCache(returnResult: true);
        var cache2 = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1)
            .AddQueryCache(_ => cache2)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache1.Reads);
        Assert.Empty(cache2.Reads);
    }

    [Fact]
    public async Task SkipReadIfShouldReadResultFromCacheReturnsFalse()
    {
        var cache = new QueryCache(skipRead: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Empty(cache.Reads);
        Assert.Single(cache.Writes);
    }

    [Fact]
    public async Task IgnoreExceptionInShouldReadFromCache()
    {
        var cache = new QueryCache(throwInShouldRead: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Empty(cache.Reads);
        Assert.Single(cache.Writes);
    }

    [Fact]
    public async Task IgnoreExceptionInReadCachedQueryResult()
    {
        var cache = new QueryCache(throwInRead: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Empty(cache.Reads);
        Assert.Single(cache.Writes);
    }

    [Fact]
    public async Task WriteToAllCaches()
    {
        var cache1 = new QueryCache();
        var cache2 = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1)
            .AddQueryCache(_ => cache2)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache1.Writes);
        Assert.Single(cache2.Writes);
    }

    [Fact]
    public async Task DoNotWriteToCacheIfNoMaxAgeCouldBeComputed()
    {
        var cache = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ __typename }");

        Assert.Null(result.Errors);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task SkipWriteIfShouldWriteResultToCacheReturnsFalse()
    {
        var cache = new QueryCache(skipWrite: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache.Reads);
        Assert.Collection(cache.ShouldWrites, result => Assert.False(result));
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task IgnoreExceptionInShouldWriteToCache()
    {
        var cache = new QueryCache(throwInShouldWrite: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache.Reads);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task IgnoreExceptionInCacheQueryResult()
    {
        var cache = new QueryCache(throwInWrite: true);

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<StringType>().CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Single(cache.Reads);
        Assert.Empty(cache.Writes);
    }
}