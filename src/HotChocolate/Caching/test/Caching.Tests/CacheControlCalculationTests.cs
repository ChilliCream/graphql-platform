using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlCalculationTests : CacheControlTestBase
{
    #region Single field
    [Fact]
    public async Task SingleField_WithoutCacheControl_Ignore()
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
    public async Task SingleField_WithCacheControl_Cache()
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
    public async Task SingleField_CacheControlOnObjectType_Cache()
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
    public async Task SingleField_CacheControlOnInterfaceType_Cache()
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
    public async Task SingleField_CacheControlOnUnionType_Cache()
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

    [Fact]
    public async Task SingleField_CacheControlOnObjectTypeAndField_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: ObjectType @cacheControl(maxAge: 50)
            }

            type ObjectType @cacheControl(maxAge: 100) {
                field: String
            }
        ");

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task SingleField_CacheControlOnInterfaceTypeAndField_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: InterfaceType @cacheControl(maxAge: 50)
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
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task SingleField_CacheControlOnUnionTypeAndField_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field: UnionType @cacheControl(maxAge: 50)
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
            result => result.MaxAge == 50);
    }
    #endregion

    #region Multiple fields
    [Fact]
    public async Task MultipleFields_OneWithOneWithoutCacheControl_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field1: String
                field2: String @cacheControl(maxAge: 100)
            }
        ");

        await ExecuteRequestAsync(builder, "{ field1 field2 }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task MultipleFields_DifferentCacheControls_Cache()
    {
        var schema = @"
            type Query {
                field1: String @cacheControl(maxAge: 50)
                field2: String @cacheControl(maxAge: 100)
            }
        ";

        var (builder1, cache1) = GetExecutorBuilderAndCache();
        builder1.AddDocumentFromString(schema);

        var (builder2, cache2) = GetExecutorBuilderAndCache();
        builder2.AddDocumentFromString(schema);

        await ExecuteRequestAsync(builder1, "{ field1 field2 }");
        await ExecuteRequestAsync(builder2, "{ field2 field1 }");

        AssertOneWriteToCache(cache1,
            result => result.MaxAge == 50);
        AssertOneWriteToCache(cache2,
            result => result.MaxAge == 50);
    }
    #endregion

    #region Deeply nested fields
    // TODO: add
    #endregion

    #region Special cases
    [Fact]
    public async Task MultipleOperations_DifferentCacheControlInOperations_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field1: String @cacheControl(maxAge: 50)
                field2: String @cacheControl(maxAge: 100)
            }
        ");

        IQueryRequest request = QueryRequestBuilder.New()
                    .SetQuery(@"
                         query First {
                             field1
                         }

                         query Second {
                            field2
                         }
                     ")
                    .SetOperation("Second")
                    .Create();

        IExecutionResult result = await builder.ExecuteRequestAsync(request);
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    public async Task CacheControlInFragment_Cache()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddDocumentFromString(@"
            type Query {
                field1: String @cacheControl(maxAge: 50)
                field2: String @cacheControl(maxAge: 100)
            }
        ");

        await ExecuteRequestAsync(builder, @"{
                fiedl2
                ...Fragment
            }

            fragment Fragment on Query {
                field1
            }
        ");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }
    #endregion

    #region Introspection queries
    [Fact]
    public async Task PureIntrospectionQuery_Ignore()
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
    public async Task IntrospectionAndRegularQuery_Ignore()
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
    public async Task TypeNameIntrospection_Cache()
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
}
