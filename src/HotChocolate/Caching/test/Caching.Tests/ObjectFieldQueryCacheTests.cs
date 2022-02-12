using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ChilliCream.Testing;

namespace HotChocolate.Caching.Tests;

public class ObjectFieldQueryCacheTests
{
    // todo: nested objects

    [Fact]
    public async Task OneField_Default()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task Introspection()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ __typename }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task OneField_MaxAge()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ maxAge1 }");

        Assert.Null(result.Errors);
        Assert.Equal(1, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task OneField_MaxAge_MultipleOperations()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);

        IQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery(@"
                        query First {
                            maxAge1
                            maxAge2
                        }

                        query Second {
                            maxAge2
                        }
                    ")
                    .SetOperation("Second")
                    .Create();

        IExecutionResult result = await executor.ExecuteAsync(request);

        Assert.Null(result.Errors);
        Assert.Equal(2, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task OneField_ScopePrivate()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    }

    [Fact]
    public async Task OneField_Scope_MultipleOperations()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);

        IQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery(@"
                        query First {
                            maxAge1
                            maxAge2
                        }

                        query Second {
                            scopePrivate
                        }
                    ")
                    .SetOperation("Second")
                    .Create();

        IExecutionResult result = await executor.ExecuteAsync(request);

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    }

    [Fact]
    public async Task TwoFields_OneMaxAge_OneDefault()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ field maxAge1 }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task TwoFields_OneScopePrivate_OneDefault()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ field scopePrivate }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    }

    [Fact]
    public async Task TwoFields_DifferentMaxAge()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ maxAge1 maxAge2 }");

        Assert.Null(result.Errors);
        Assert.Equal(1, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task TwoFields_DifferentMaxAge_Fragment()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync(@"
            { 
                maxAge2 
                ...QueryFragment 
            }
            
            fragment QueryFragment on Query {
                maxAge1
            }
            ");

        Assert.Null(result.Errors);
        Assert.Equal(1, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    }

    [Fact]
    public async Task TwoFields_DifferentScope()
    {
        var cache = new TestQueryCache();

        IRequestExecutor executor = await GetTestExecutorAsync(cache);
        IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate scopePublic }");

        Assert.Null(result.Errors);
        Assert.Equal(0, cache.Result?.MaxAge);
        Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    }

    private static async Task<IRequestExecutor> GetTestExecutorAsync(IQueryCache cache)
    {
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(FileResource.Open("CacheControlSchema.graphql"))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .BuildRequestExecutorAsync();

        return executor;
    }

    private class TestQueryCache : DefaultQueryCache
    {
        public CacheControlResult? Result { get; set; }

        public override Task CacheQueryResultAsync(IRequestContext context, CacheControlResult result,
            ICacheControlOptions options)
        {
            Result = result;

            return Task.CompletedTask;
        }

        public override Task<IQueryResult?> TryReadCachedQueryResultAsync(IRequestContext context,
            ICacheControlOptions options)
        {
            return Task.FromResult<IQueryResult?>(null);
        }
    }
}