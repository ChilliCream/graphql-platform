using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
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

        IExecutionResult result = await executor.ExecuteAsync("{ scalar_fieldCache }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        Assert.NotNull(cache.Options);
        Assert.True(cache.Options!.Enable);
        Assert.Equal(0, cache.Options!.DefaultMaxAge);
        Assert.True(cache.Options!.ApplyDefaults);
        //Assert.Null(cache.Options!.GetSessionId);
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
                //options.GetSessionId = context => "Test";
            })
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ scalar_fieldCache }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        Assert.NotNull(cache.Options);
        Assert.True(cache.Options!.Enable);
        Assert.Equal(100, cache.Options!.DefaultMaxAge);
        Assert.False(cache.Options!.ApplyDefaults);
        //Assert.NotNull(cache.Options!.GetSessionId);
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

        IExecutionResult result = await executor.ExecuteAsync("{ scalar_fieldCache }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        Assert.NotNull(cache.Options);
        Assert.Equal(10, cache.Options!.DefaultMaxAge);
        Assert.False(cache.Options!.ApplyDefaults);
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

        IExecutionResult result = await executor.ExecuteAsync("{ scalar_fieldCache }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        Assert.NotNull(cache.Options);
        Assert.Equal(10, cache.Options!.DefaultMaxAge);
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

        IExecutionResult result = await executor.ExecuteAsync("{ scalar_fieldCache }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        Assert.Null(cache.Options);
    }

    [Fact]
    public async Task CacheControlOptionsAreScopedToSchema()
    {
        var cache1 = new TestQueryCache();
        var cache2 = new TestQueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer("schema1")
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o =>
            {
                o.ApplyDefaults = false;
                o.DefaultMaxAge = 100;
            })
            .AddGraphQLServer("schema2")
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache2)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o =>
            {
                o.ApplyDefaults = false;
                o.DefaultMaxAge = 200;
            });

        IRequestExecutor executor1 = await builder.BuildRequestExecutorAsync("schema1");
        IExecutionResult executor1Result = await executor1.ExecuteAsync("{ a: scalar_fieldCache }");
        IQueryResult executor1QueryResult = executor1Result.ExpectQueryResult();

        IRequestExecutor executor2 = await builder.BuildRequestExecutorAsync("schema2");
        IExecutionResult executor2Result = await executor2.ExecuteAsync("{ b: scalar_fieldCache }");
        IQueryResult executor2QueryResult = executor2Result.ExpectQueryResult();

        Assert.Null(executor1QueryResult.Errors);
        Assert.NotNull(cache1.Options);
        Assert.Equal(100, cache1.Options!.DefaultMaxAge);

        Assert.Null(executor2QueryResult.Errors);
        Assert.NotNull(cache2.Options);
        Assert.Equal(200, cache2.Options!.DefaultMaxAge);
    }

    private class TestQueryCache : DefaultQueryCache
    {
        public ICacheControlOptions? Options { get; private set; }

        public override Task CacheQueryResultAsync(IRequestContext context, ICacheControlResult result,
            ICacheControlOptions options)
        {
            Options = options;

            return Task.CompletedTask;
        }
    }
}
