using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlCalculationTests : CacheControlTestBase
{
    #region Single field
    [Fact]
    public async Task SingleField_WithoutCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task SingleField_WithCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: String @cacheControl(maxAge: 100)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneWriteToCache(cache,
            r => r.MaxAge == 100 && r.Scope == CacheControlScope.Public);
    }

    [Fact]
    public async Task SingleField_WithCacheControlAndScope()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: String @cacheControl(maxAge: 100 scope: PRIVATE)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field }");

        AssertOneWriteToCache(cache,
            r => r.MaxAge == 100 && r.Scope == CacheControlScope.Private);
    }

    [Fact]
    public async Task SingleFieldControlOnObjectType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType
                }

                type ObjectType @cacheControl(maxAge: 100) {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task SingleFieldControlOnInterfaceType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: InterfaceType
                }

                interface InterfaceType @cacheControl(maxAge: 100) {
                    field: String
                }

                type ObjectType implements InterfaceType {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task SingleFieldControlOnUnionType()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: UnionType
                }

                union UnionType @cacheControl(maxAge: 100) = ObjectType

                type ObjectType {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

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
    public async Task SingleFieldControlOnObjectTypeAndField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType @cacheControl(maxAge: 50)
                }

                type ObjectType @cacheControl(maxAge: 100) {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task SingleFieldControlOnInterfaceTypeAndField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: InterfaceType @cacheControl(maxAge: 50)
                }

                interface InterfaceType @cacheControl(maxAge: 100) {
                    field: String
                }

                type ObjectType implements InterfaceType {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task SingleFieldControlOnUnionTypeAndField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: UnionType @cacheControl(maxAge: 50)
                }

                union UnionType @cacheControl(maxAge: 100) = ObjectType

                type ObjectType {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

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

    [Fact]
    public async Task MergeScopeFromTypeIfMissingOnField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType @cacheControl(maxAge: 50)
                }

                type ObjectType @cacheControl(scope: PRIVATE) {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            r => r.MaxAge == 50 && r.Scope == CacheControlScope.Private);
    }

    [Fact]
    public async Task MergeMaxAgeFromTypeIfMissingOnField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType @cacheControl(scope: PRIVATE)
                }

                type ObjectType @cacheControl(maxAge: 50) {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            r => r.MaxAge == 50 && r.Scope == CacheControlScope.Private);
    }

    [Fact]
    public async Task DoNotMergeMaxAgeFromTypeIfInheritMaxAgeOnField()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType @cacheControl(maxAge: 100)
                }

                type ObjectType {
                    field: ObjectType2 @cacheControl(inheritMaxAge: true)
                }

                type ObjectType2 @cacheControl(maxAge: 50) {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field { field } } }");

        AssertOneWriteToCache(cache, r => r.MaxAge == 100);
    }
    #endregion

    #region Multiple fields
    [Fact]
    public async Task MultipleFields_OneWithOneWithoutCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field1: String
                    field2: String @cacheControl(maxAge: 100)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field1 field2 }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task MultipleFields_DifferentCacheControls()
    {
        var schema = @"
            type Query {
                field1: String @cacheControl(maxAge: 50)
                field2: String @cacheControl(maxAge: 100)
            }
        ";

        var (builder1, cache1) = GetExecutorBuilderAndCache();
        builder1
            .AddDocumentFromString(schema)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var (builder2, cache2) = GetExecutorBuilderAndCache();
        builder2
            .AddDocumentFromString(schema)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder1, "{ field1 field2 }");
        await ExecuteRequestAsync(builder2, "{ field2 field1 }");

        AssertOneWriteToCache(cache1,
            result => result.MaxAge == 50);
        AssertOneWriteToCache(cache2,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task MultipleFields_DifferentCacheControlAndScopes()
    {
        var schema = @"
            type Query {
                field1: String @cacheControl(maxAge: 50)
                field2: String @cacheControl(maxAge: 100 scope: PRIVATE)
            }
        ";

        var (builder1, cache1) = GetExecutorBuilderAndCache();
        builder1
            .AddDocumentFromString(schema)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var (builder2, cache2) = GetExecutorBuilderAndCache();
        builder2
            .AddDocumentFromString(schema)
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder1, "{ field1 field2 }");
        await ExecuteRequestAsync(builder2, "{ field2 field1 }");

        AssertOneWriteToCache(cache1,
            r => r.MaxAge == 50 && r.Scope == CacheControlScope.Private);
        AssertOneWriteToCache(cache2,
            r => r.MaxAge == 50 && r.Scope == CacheControlScope.Private);
    }
    #endregion

    #region Deeply nested fields
    [Fact]
    public async Task DeepestLeafNodeIsNotResult()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field1: String @cacheControl(maxAge: 200)
                    field2: Obj1 @cacheControl(maxAge: 100)
                }

                type Obj1 {
                    field1: String @cacheControl(maxAge: 1)
                    field2: Obj2 @cacheControl(maxAge: 75 scope: PRIVATE)
                }

                type Obj2 {
                    field: String @cacheControl(maxAge: 50 scope: PUBLIC)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, @"{
            field1
            field2 {
                field1
                field2 {
                    field
                }
            }
        }");

        AssertOneWriteToCache(cache,
            r => r.MaxAge == 1 && r.Scope == CacheControlScope.Private);
    }

    [Fact]
    public async Task FieldOnNestedObjectTypeHasCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: ObjectType @cacheControl(maxAge: 100)
                }

                type ObjectType {
                    field: String @cacheControl(maxAge: 50)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task FieldOnNestedInterfaceTypeHasCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: InterfaceType @cacheControl(maxAge: 100)
                }

                interface InterfaceType {
                    field: String
                }

                type ObjectType implements InterfaceType {
                    field: String @cacheControl(maxAge: 50)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, "{ field { field } }");

        AssertOneWriteToCache(cache,
            result => result.MaxAge == 50);
    }

    [Fact]
    public async Task FieldOnNestedUnionTypeHasCacheControl()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: UnionType @cacheControl(maxAge: 100)
                }

                union UnionType = ObjectType

                type ObjectType {
                    field: String @cacheControl(maxAge: 50)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

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

    #region Special queries
    [Fact]
    public async Task MultipleOperations_DifferentCacheControlInOperations()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field1: String @cacheControl(maxAge: 50)
                    field2: String @cacheControl(maxAge: 100)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var request = QueryRequestBuilder.New()
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

        var result = await builder.ExecuteRequestAsync(request);
        var queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);
        AssertOneWriteToCache(cache,
            result => result.MaxAge == 100);
    }

    [Fact]
    public async Task CacheControlInFragment()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field1: String @cacheControl(maxAge: 50)
                    field2: String @cacheControl(maxAge: 100)
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, @"{
                field2
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
    public async Task PureIntrospectionQuery()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, @"{ __schema { types { name } } }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task IntrospectionAndRegularQuery()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, @"{ __schema { types { name } } field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task TypeNameIntrospection()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .AddDocumentFromString(@"
                type Query {
                    field: UnionType @cacheControl(maxAge: 100)
                }

                union UnionType = ObjectType

                type ObjectType {
                    field: String
                }
            ")
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        await ExecuteRequestAsync(builder, @"{
            field {
                __typename
            }
        }");

        AssertOneWriteToCache(cache);
    }
    #endregion
}
