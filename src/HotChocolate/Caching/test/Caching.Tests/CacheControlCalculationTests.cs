using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlCalculationTests : CacheControlTestBase
{
    #region Single field
    [Fact]
    public async Task Ignore_SingleField_WithoutCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task Cache_SingleField_WithCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: String @cacheControl(maxAge: 100)
            }
        ");

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task Cache_SingleField_CacheControlOnObjectType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: ObjectType
            }

            type ObjectType @cacheControl(maxAge: 100) {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task Cache_SingleField_CacheControlOnInterfaceType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: InterfaceType
            }

            interface InterfaceType @cacheControl(maxAge: 100) {
                field: String
            }

            type ObjectType implements InterfaceType {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task Cache_SingleField_CacheControlOnUnionType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: UnionType
            }

            union UnionType @cacheControl(maxAge: 100) = ObjectType

            type ObjectType {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, @"{
            field {
                ... on ObjectType {
                    field
                }
            }
        }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }
    #endregion

    #region Introspection queries
    [Fact]
    public async Task Ignore_Pure_Introspection_Query()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, @"{ __schema { types { name } } }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task Ignore_IntrospectionAndRegularQuery_OnSameLevel()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, @"{ __schema { types { name } } field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task Cache_TypeNameIntrospection()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: UnionType @cacheControl(maxAge: 100)
            }

            union UnionType = ObjectType

            type ObjectType {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, @"{
            field {
                __typename
            }
        }");

        AssertOneWriteToCache(cache);
    }
    #endregion

    //[Fact]
    //public async Task FieldAndTypeHaveCacheControl()
    //{
    //    await AssertOneWriteToCacheAsync("{ fieldAndTypeCache { field } }",
    //        result => result.MaxAge == 2 && result.Scope == CacheControlScope.Private);
    //}

    //[Fact]
    //public async Task TwoFields_OneMaxAge_OneDefault()
    //{
    //    await AssertOneWriteToCacheAsync("{ regular fieldCache }",
    //        result => result.MaxAge == 1 && result.Scope == CacheControlScope.Private);
    //}

    //[Fact]
    //public async Task TwoFields_OneScopePrivate_OneDefault()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //    IExecutionResult result = await executor.ExecuteAsync("{ field scopePrivate }");

    //    Assert.Null(result.Errors);
    //    Assert.Equal(0, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task TwoFields_DifferentMaxAge()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //    IExecutionResult result = await executor.ExecuteAsync("{ maxAge1 maxAge2 }");

    //    Assert.Null(result.Errors);
    //    Assert.Equal(1, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task TwoFields_DifferentMaxAge_Fragment()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //    IExecutionResult result = await executor.ExecuteAsync(@"
    //         { 
    //             maxAge2 
    //             ...QueryFragment 
    //         }

    //         fragment QueryFragment on Query {
    //             maxAge1
    //         }
    //         ");

    //    Assert.Null(result.Errors);
    //    Assert.Equal(1, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task TwoFields_DifferentScope()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //    IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate scopePublic }");

    //    Assert.Null(result.Errors);
    //    Assert.Equal(0, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task OneField_MaxAge_MultipleOperations()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);

    //    IQueryRequest request = QueryRequestBuilder.New()
    //                .SetQuery(@"
    //                     query First {
    //                         maxAge1
    //                         maxAge2
    //                     }

    //                     query Second {
    //                         maxAge2
    //                     }
    //                 ")
    //                .SetOperation("Second")
    //                .Create();

    //    IExecutionResult result = await executor.ExecuteAsync(request);

    //    Assert.Null(result.Errors);
    //    Assert.Equal(2, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Public, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task OneField_ScopePrivate()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);
    //    IExecutionResult result = await executor.ExecuteAsync("{ scopePrivate }");

    //    Assert.Null(result.Errors);
    //    Assert.Equal(0, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    //}

    //[Fact]
    //public async Task OneField_Scope_MultipleOperations()
    //{
    //    var cache = new TestQueryCache();

    //    IRequestExecutor executor = await GetTestExecutorAsync(cache);

    //    IQueryRequest request = QueryRequestBuilder.New()
    //                .SetQuery(@"
    //                     query First {
    //                         maxAge1
    //                         maxAge2
    //                     }

    //                     query Second {
    //                         scopePrivate
    //                     }
    //                 ")
    //                .SetOperation("Second")
    //                .Create();

    //    IExecutionResult result = await executor.ExecuteAsync(request);

    //    Assert.Null(result.Errors);
    //    Assert.Equal(0, cache.Result?.MaxAge);
    //    Assert.Equal(CacheControlScope.Private, cache.Result?.Scope);
    //}
}
