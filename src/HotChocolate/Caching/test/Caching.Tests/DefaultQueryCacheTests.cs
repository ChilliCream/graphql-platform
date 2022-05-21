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
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("").CacheControl(100))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertOneWriteToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheResultWithErrors()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<NonNullType<StringType>>().CacheControl(100)
                    .Resolve(context =>
                    {
                        throw new Exception();
                    }))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.NotEmpty(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheDeferredResults()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ ... @defer { deferred } regular }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheBatchedResults()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("").CacheControl(100))
            .AddQueryCache(_ => cache.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IQueryRequest request1 = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .Create();

        IQueryRequest request2 = QueryRequestBuilder.New()
            .SetQuery("{ field }")
            .Create();

        var requestBatch = new[] { request1, request2 };

        IExecutionResult result = await executor.ExecuteBatchAsync(requestBatch);

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task DoNotCacheMutationResults()
    {
        Mock<DefaultQueryCache> cache = GetMockedCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
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

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("mutation { modifyEntity { field } }");
        IQueryResult queryResult = result.ExpectQueryResult();

        Assert.Null(queryResult.Errors);

        AssertNoWritesToCache(cache);
    }
}
