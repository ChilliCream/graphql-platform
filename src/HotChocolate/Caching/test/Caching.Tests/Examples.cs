using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Caching.Tests;

[CacheControl(10)]
public class AnnotationBasedObjectType
{
    [CacheControl(MaxAge = 5, Scope = CacheControlScope.Private)]
    public string Field { get; set; }

    public string DynamicField(IResolverContext context)
    {
        context.CacheControl(10);
        context.CacheControl(scope: CacheControlScope.Private);
        context.CacheControl(10, CacheControlScope.Private);

        return string.Empty;
    }
}

public class CodeFirstObjectType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.CacheControl(10);

        descriptor
            .Field("field")
            .CacheControl(5)
            .CacheControl(maxAge: 2)
            .CacheControl(scope: CacheControlScope.Private)
            .CacheControl(5, CacheControlScope.Private);
    }
}

public class CodeFirstInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.CacheControl(10);

        descriptor
            .Field("field")
            .CacheControl(5)
            .CacheControl(maxAge: 2)
            .CacheControl(scope: CacheControlScope.Private)
            .CacheControl(5, CacheControlScope.Private);
    }
}

public class CodeFirstUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.CacheControl(10);
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                    field1: String @cacheControl(maxAge: 10)
                    field2: String @cacheControl(scope: PRIVATE)
                    field3: String @cacheControl(inheritMaxAge: true)
                }
            ")
            .AddQueryCache<MyQueryCache>(settings =>
            {
                settings.Enable = true;
                settings.DefaultMaxAge = 2000;
                settings.GetSessionId = context =>
                {
                    if (context.ContextData.TryGetValue("sessionId", out var sessionId))
                    {
                        return sessionId;
                    }

                    return null;
                };
            })
            .UseQueryResultCachePipeline();
    }
}

public class MyQueryCache : DefaultQueryCache
{
    // optional
    public override bool ShouldReadResultFromCache(IRequestContext context)
    {
        return base.ShouldReadResultFromCache(context);
    }

    // optional
    public override bool ShouldWriteResultToCache(IRequestContext context)
    {
        return base.ShouldWriteResultToCache(context);
    }

    public override Task CacheQueryResultAsync(IRequestContext context,
        QueryCacheResult result, IQueryCacheSettings settings)
    {
        var sessionId = settings.GetSessionId(context);
        var maxAge = result.MaxAge;
        var scope = result.Scope;

        // create a cacheId and store the document

        throw new System.NotImplementedException();
    }

    public override Task<IQueryResult> TryReadCachedQueryResultAsync(
        IRequestContext context, IQueryCacheSettings settings)
    {
        // create a cacheId and lookup the document

        throw new System.NotImplementedException();
    }
}