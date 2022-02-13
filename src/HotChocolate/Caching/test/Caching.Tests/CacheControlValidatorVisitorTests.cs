using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlValidatorVisitorTests : CacheControlTestBase
{
    // todo: add way more tests for all common scenarios

    [Fact]
    public async Task Ignore_Introspection()
    {
        CacheControlResult result = await ValidateResultAsync("{ __typename }");

        Assert.Equal(0, result.MaxAge);
        Assert.Equal(CacheControlScope.Public, result.Scope);
    }

    [Fact]
    public async Task NoCacheControl()
    {
        CacheControlResult result = await ValidateResultAsync("{ regular }");

        Assert.Equal(0, result.MaxAge);
        Assert.Equal(CacheControlScope.Public, result.Scope);
    }

    [Fact]
    public async Task FieldHasCacheControl()
    {
        CacheControlResult result = await ValidateResultAsync("{ fieldCache }");

        Assert.Equal(1, result.MaxAge);
        Assert.Equal(CacheControlScope.Private, result.Scope);
    }

    [Fact]
    public async Task TypeHasCacheControl()
    {
        CacheControlResult result = await ValidateResultAsync("{ typeCache { field } }");

        Assert.Equal(180, result.MaxAge);
        Assert.Equal(CacheControlScope.Private, result.Scope);
    }

    [Fact]
    public async Task FieldAndTypeHaveCacheControl()
    {
        CacheControlResult result = await ValidateResultAsync("{ fieldAndTypeCache { field } }");

        Assert.Equal(2, result.MaxAge);
        Assert.Equal(CacheControlScope.Private, result.Scope);
    }

    // [Fact]
    // public async Task OneField_MaxAge_MultipleOperations()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);

    //     IQueryRequest request = QueryRequestBuilder.New()
    //                 .SetQuery(@"
    //                     query First {
    //                         maxAge1
    //                         maxAge2
    //                     }

    //                     query Second {
    //                         maxAge2
    //                     }
    //                 ")
    //                 .SetOperation("Second")
    //                 .Create();

    //     IExecutionResult result = await executor.ExecuteAsync(request);

    //     Assert.Null(result.Errors);
    //     Assert.Equal(2, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task OneField_ScopePrivate()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate }");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(0, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task OneField_Scope_MultipleOperations()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);

    //     IQueryRequest request = QueryRequestBuilder.New()
    //                 .SetQuery(@"
    //                     query First {
    //                         maxAge1
    //                         maxAge2
    //                     }

    //                     query Second {
    //                         scopePrivate
    //                     }
    //                 ")
    //                 .SetOperation("Second")
    //                 .Create();

    //     IExecutionResult result = await executor.ExecuteAsync(request);

    //     Assert.Null(result.Errors);
    //     Assert.Equal(0, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task TwoFields_OneMaxAge_OneDefault()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync("{ field maxAge1 }");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(0, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task TwoFields_OneScopePrivate_OneDefault()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync("{ field scopePrivate }");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(0, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task TwoFields_DifferentMaxAge()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync("{ maxAge1 maxAge2 }");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(1, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task TwoFields_DifferentMaxAge_Fragment()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync(@"
    //         { 
    //             maxAge2 
    //             ...QueryFragment 
    //         }

    //         fragment QueryFragment on Query {
    //             maxAge1
    //         }
    //         ");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(1, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    // }

    // [Fact]
    // public async Task TwoFields_DifferentScope()
    // {
    //     var cache = new TestQueryCache();

    //     IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //     IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate scopePublic }");

    //     Assert.Null(result.Errors);
    //     Assert.Equal(0, cache.Result?.MaxAge);
    //     Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    // }
}