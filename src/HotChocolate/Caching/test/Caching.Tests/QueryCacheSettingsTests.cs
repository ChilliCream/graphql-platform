using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class QueryCacheSettingsTests
{
    [Fact]
    public async Task QueryCacheSettings_Default()
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
        Assert.NotNull(cache.Settings);
        Assert.True(cache.Settings.Enable);
        Assert.Equal(0, cache.Settings.DefaultMaxAge);
        Assert.Null(cache.Settings.GetSessionId);
    }

    [Fact]
    public async Task QueryCacheSettings_Modified()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyQueryCacheOptions(options =>
            {
                options.GetSessionId = context => "Test";
                options.DefaultMaxAge = 100;
            })
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Settings);
        Assert.True(cache.Settings.Enable);
        Assert.Equal(100, cache.Settings.DefaultMaxAge);
        Assert.NotNull(cache.Settings.GetSessionId);
    }

    [Fact]
    public async Task QueryCacheSettings_Modified_AfterPipeline()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyQueryCacheOptions(options => options.DefaultMaxAge = 10)
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.NotNull(cache.Settings);
        Assert.Equal(10, cache.Settings.DefaultMaxAge);
    }

    [Fact]
    public async Task QueryCacheSettings_Disable()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .ModifyQueryCacheOptions(options => options.Enable = false)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Null(cache.Settings);
    }

    private class TestQueryCache : DefaultQueryCache
    {
        public IQueryCacheSettings? Settings { get; private set; }

        public override Task CacheQueryResultAsync(IRequestContext context, CacheControlResult result, IQueryCacheSettings settings)
        {
            Settings = settings;

            return Task.CompletedTask;
        }

        public override Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context, IQueryCacheSettings settings)
        {
            return Task.FromResult<IQueryResult?>(null);
        }
    }
}