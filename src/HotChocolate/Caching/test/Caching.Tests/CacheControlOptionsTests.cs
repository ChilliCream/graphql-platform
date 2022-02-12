using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlOptionsTests
{
    [Fact]
    public async Task CacheControlOptions_Default()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Options);
        Assert.True(cache.Options?.Enable);
        Assert.Equal(0, cache.Options?.DefaultMaxAge);
        Assert.True(cache.Options?.ApplyDefaults);
        Assert.Null(cache.Options?.GetSessionId);
    }

    [Fact]
    public async Task CacheControlOptions_Modified()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(options =>
            {
                options.DefaultMaxAge = 100;
                options.ApplyDefaults = false;
                options.GetSessionId = context => "Test";
            })
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Options);
        Assert.True(cache.Options?.Enable);
        Assert.Equal(100, cache.Options?.DefaultMaxAge);
        Assert.False(cache.Options?.ApplyDefaults);
        Assert.NotNull(cache.Options?.GetSessionId);
    }

    [Fact]
    public async Task CacheControlOptions_Modified_Twice()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .ModifyCacheControlOptions(options => options.DefaultMaxAge = 10)
            .ModifyCacheControlOptions(options => options.ApplyDefaults = false)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Options);
        Assert.Equal(10, cache.Options?.DefaultMaxAge);
        Assert.False(cache.Options?.ApplyDefaults);
    }

    [Fact]
    public async Task CacheControlOptions_Modified_AfterRegistrations()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(options => options.DefaultMaxAge = 10)
            .UseQueryCachePipeline()
            .AddQueryCache(_ => cache)
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Options);
        Assert.Equal(10, cache.Options?.DefaultMaxAge);
    }

    [Fact]
    public async Task CacheControlOptions_Disable()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyCacheControlOptions(options => options.Enable = false)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Null(cache.Options);
    }

    private class TestQueryCache : DefaultQueryCache
    {
        public ICacheControlOptions? Options { get; private set; }

        public override Task CacheQueryResultAsync(IRequestContext context, CacheControlResult result,
            ICacheControlOptions options)
        {
            Options = options;

            return Task.CompletedTask;
        }

        public override Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
            ICacheControlOptions options)
        {
            return Task.FromResult<IQueryResult?>(null);
        }
    }
}