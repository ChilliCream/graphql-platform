using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotChocolate.Caching.Tests;

public class CacheControlOptionsTests : CacheControlTestBase
{
    [Fact]
    public async Task CacheControlOptions_Default()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder.AddQueryType(d => d.Name("Query")
            .Field("field").Resolve("")
            .CacheControl(100));

        await ExecuteRequestAsync(builder, "{ field }");

        cache.Verify(x => x.CacheQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlResult>(),
                It.Is<ICacheControlOptions>(o =>
                    o.Enable == true &&
                    o.ApplyDefaults == true &&
                    o.DefaultMaxAge == 0)),
            Times.Once());
    }

    [Fact]
    public async Task CacheControlOptions_Modified()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .ModifyCacheControlOptions(options =>
            {
                options.ApplyDefaults = false;
                options.DefaultMaxAge = 100;
                options.GetSessionId = context => "Test";
            })
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("")
                .CacheControl(100));

        await ExecuteRequestAsync(builder, "{ field }");

        cache.Verify(x => x.CacheQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlResult>(),
                It.Is<ICacheControlOptions>(o =>
                    o.Enable == true &&
                    o.ApplyDefaults == false &&
                    o.DefaultMaxAge == 100 &&
                    o.GetSessionId != null)),
            Times.Once());
    }

    [Fact]
    public async Task CacheControlOptions_ModifiedTwice()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .ModifyCacheControlOptions(options => options.DefaultMaxAge = 10)
            .ModifyCacheControlOptions(options => options.ApplyDefaults = false)
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("")
                .CacheControl(100));

        await ExecuteRequestAsync(builder, "{ field }");

        cache.Verify(x => x.CacheQueryResultAsync(
                It.IsAny<IRequestContext>(),
                It.IsAny<ICacheControlResult>(),
                It.Is<ICacheControlOptions>(o =>
                    o.Enable == true &&
                    o.ApplyDefaults == false &&
                    o.DefaultMaxAge == 10)),
            Times.Once());
    }

    [Fact]
    public async Task CacheControlOptions_Disable()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .ModifyCacheControlOptions(o => o.Enable = false)
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("")
                .CacheControl(100));

        await ExecuteRequestAsync(builder, "{ field }");

        AssertNoWritesToCache(cache);
    }

    [Fact]
    public async Task CacheControlOptionsAreScopedToSchema()
    {
        var cache1 = GetMock();
        var cache2 = GetMock();

        var schema = @"
            type Query {
                field: String
            }
        ";

        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQLServer("schema1")
            .AddDocumentFromString(schema)
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache1.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.DefaultMaxAge = 100)
            .AddGraphQLServer("schema2")
            .AddDocumentFromString(schema)
            .UseField(_ => _ => default)
            .AddQueryCache(_ => cache2.Object)
            .UseQueryCachePipeline()
            .ModifyCacheControlOptions(o => o.DefaultMaxAge = 200);

        await ExecuteRequestAsync(builder, "schema1", "{ field }");
        await ExecuteRequestAsync(builder, "schema2", "{ field }");

        AssertOneWriteToCache(cache1, r => r.MaxAge == 100);
        AssertOneWriteToCache(cache2, r => r.MaxAge == 200);
    }
}
