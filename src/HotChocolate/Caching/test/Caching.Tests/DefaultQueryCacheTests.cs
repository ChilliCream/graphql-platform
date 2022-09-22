using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class DefaultQueryCacheTests : CacheControlTestBase
{
    [Fact]
    public async Task CacheRegularResult()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("").CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("{ field }");
        var queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertOneWriteToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheResultWithErrors()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d
                .Name("Query")
                .Field("field")
                .Type<NonNullType<StringType>>()
                .CacheControl(100)
                .Resolve(_ => throw new Exception()))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("{ field }");
        var queryResult = result.ExpectQueryResult();

        Assert.NotEmpty(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheDeferredResults()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("deferred").Type<StringType>().CacheControl(100)
                    .Resolve(async context =>
                    {
                        await Task.Delay(500);

                        return "Deferred";
                    });
                d.Field("regular").Resolve("Regular");
            })
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("{ ... @defer { deferred } regular }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheBatchedResults()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("").CacheControl(100))
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var request1 = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .Create();

        var request2 = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .Create();

        var requestBatch = new[] { request1, request2 };

        IExecutionResult result = await executor.ExecuteBatchAsync(requestBatch);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheMutationResults()
    {
        var cache = GetMock();

        var builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                    field: String
                }

                type Mutation {
                    modifyEntity: CachableEntity
                }

                type CachableEntity {
                    field: String @cacheControl(maxAge: 100)
                }
            ")
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        var executor = await builder.BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync("mutation { modifyEntity { field } }");
        var queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }
}
