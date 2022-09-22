using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
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

        var schema = await builder.BuildSchemaAsync();

        var accessor = schema.Services?
            .GetRequiredService<ICacheControlOptionsAccessor>();

        Assert.NotNull(accessor);
        Assert.True(accessor!.CacheControl.Enable);
        Assert.True(accessor!.CacheControl.ApplyDefaults);
        Assert.Equal(0, accessor!.CacheControl.DefaultMaxAge);
        Assert.Null(accessor!.CacheControl.GetSessionId);
    }

    [Fact]
    public async Task CacheControlOptions_Modified()
    {
        var (builder, cache) = GetExecutorBuilderAndCache();

        builder
            .ModifyCacheControlOptions(options =>
            {
                options.Enable = false;
                options.ApplyDefaults = false;
                options.DefaultMaxAge = 100;
                options.GetSessionId = context => "Test";
            })
            .AddQueryType(d => d.Name("Query")
                .Field("field").Resolve("")
                .CacheControl(100));

        var schema = await builder.BuildSchemaAsync();

        var accessor = schema.Services?
            .GetRequiredService<ICacheControlOptionsAccessor>();

        Assert.NotNull(accessor);
        Assert.False(accessor!.CacheControl.Enable);
        Assert.False(accessor!.CacheControl.ApplyDefaults);
        Assert.Equal(100, accessor!.CacheControl.DefaultMaxAge);
        Assert.NotNull(accessor!.CacheControl.GetSessionId);
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

        var schema = await builder.BuildSchemaAsync();

        var accessor = schema.Services?
            .GetRequiredService<ICacheControlOptionsAccessor>();

        Assert.NotNull(accessor);
        Assert.True(accessor!.CacheControl.Enable);
        Assert.False(accessor!.CacheControl.ApplyDefaults);
        Assert.Equal(10, accessor!.CacheControl.DefaultMaxAge);
        Assert.Null(accessor!.CacheControl.GetSessionId);
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

        var builder = new ServiceCollection()
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

        var schema1 = await builder.BuildSchemaAsync("schema1");

        var accessor1 = schema1.Services?
            .GetRequiredService<ICacheControlOptionsAccessor>();

        Assert.NotNull(accessor1);
        Assert.Equal(100, accessor1!.CacheControl.DefaultMaxAge);

        var schema2 = await builder.BuildSchemaAsync("schema2");

        var accessor2 = schema2.Services?
            .GetRequiredService<ICacheControlOptionsAccessor>();

        Assert.NotNull(accessor2);
        Assert.Equal(200, accessor2!.CacheControl.DefaultMaxAge);
    }
}
