using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class DefaultQueryCacheTests : CacheControlTestBase
{
    [Fact]
    public async Task DoNotCacheResultWithErrors()
    {
        var cache = new QueryCache();

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query")
                .Field("field").Type<NonNullType<StringType>>().CacheControl(100)
                    .Resolve(context =>
                    {
                        throw new Exception();
                    }))
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ field }");

        Assert.NotEmpty(result.Errors);
        Assert.Single(cache.Reads);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task DoNotCacheDeferredResults()
    {
        var cache = new QueryCache();

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
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("{ ... @defer { deferred } regular }");

        Assert.Null(result.Errors);
        Assert.Single(cache.Reads);
        Assert.Empty(cache.Writes);
    }

    [Fact]
    public async Task DoNotCacheMutationResults()
    {
        var cache = new QueryCache();

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
            .AddQueryCache(_ => cache)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.ApplyDefaults = false);

        IRequestExecutor executor = await builder.BuildRequestExecutorAsync();

        IExecutionResult result = await executor.ExecuteAsync("mutation { modifyEntity { field } }");

        Assert.Null(result.Errors);
        Assert.Collection(cache.ShouldWrites, result => Assert.False(result));
        Assert.Single(cache.Reads);
        Assert.Empty(cache.Writes);
    }
}