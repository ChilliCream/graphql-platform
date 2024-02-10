using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Snapshooter.Xunit;

namespace HotChocolate.Resolvers;

public class ResolverServiceTests
{
    [Fact]
    public async Task AddDefaultService()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddSingleton<SayHelloService>()
            .AddGraphQL()
            .AddQueryType<QueryService>()
            .ExecuteRequestAsync("{ sayHello }")
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddPooledService()
    {
        Snapshot.FullName();

        var pooledService = new SayHelloServicePool();

        await new ServiceCollection()
            .AddSingleton<ObjectPool<SayHelloService>>(pooledService)
            .AddGraphQL()
            .AddQueryType<QueryPooledService>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync("{ sayHello }")
            .MatchSnapshotAsync();

        Assert.True(pooledService.GetService);
        Assert.True(pooledService.ReturnService);
    }

    [Fact]
    public async Task AddSynchronizedService()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddSingleton<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QuerySynchronizedService>()
                .BuildRequestExecutorAsync();

        Assert.False(executor.Schema.QueryType.Fields["sayHello"].IsParallelExecutable);

        await executor.ExecuteAsync("{ sayHello }").MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddResolverService()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddSingleton<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryResolverService>(x => x.Name("Query"))
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .MapField(
                    new FieldReference("Query", "sayHello"),
                    next => async context =>
                    {
                        await next(context);
                        Assert.True(
                            context.LocalContextData.ContainsKey(
                                WellKnownMiddleware.ResolverServiceScope));
                    })
                .BuildRequestExecutorAsync();

        await executor.ExecuteAsync("{ sayHello }").MatchSnapshotAsync();
    }

    [Fact]
    public async Task AddResolverService_2()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddSingleton<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryType>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        await executor.ExecuteAsync("{ sayHello }").MatchSnapshotAsync();
    }
    
#if NET8_0_OR_GREATER
    [Fact]
    public async Task Resolve_KeyedService()
    {
        Snapshot.FullName();

        var executor =
            await new ServiceCollection()
                .AddKeyedSingleton("abc", (_, __) => new KeyedService("abc"))
                .AddKeyedSingleton("def", (_, __) => new KeyedService("def"))
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
                .BuildRequestExecutorAsync();

        await executor.ExecuteAsync("{ foo }").MatchSnapshotAsync();
    }
#endif

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("sayHello")
                .ResolveWith<QueryResolverService>(r => r.SayHello(default!));
        }
    }

    public class SayHelloService
    {
        public string SayHello() => "Hello";
    }

    public class QueryService
    {
        public string SayHello([Service] SayHelloService service)
            => service.SayHello();
    }

    public class QueryPooledService
    {
        public string SayHello([Service(ServiceKind.Pooled)] SayHelloService service)
            => service.SayHello();
    }

    public class QuerySynchronizedService
    {
        public string SayHello([Service(ServiceKind.Synchronized)] SayHelloService service)
            => service.SayHello();
    }

    public class QueryResolverService
    {
        public string SayHello([Service(ServiceKind.Resolver)] SayHelloService service)
            => service.SayHello();
    }

    public class SayHelloServicePool : ObjectPool<SayHelloService>
    {
        public bool GetService { get; private set; }

        public bool ReturnService { get; private set; }

        public override SayHelloService Get()
        {
            GetService = true;
            return new SayHelloService();
        }

        public override void Return(SayHelloService obj)
        {
            ReturnService = true;
        }
    }

#if NET8_0_OR_GREATER
    public class Query
    {
        public string Foo([AbcService] KeyedService service)
            => service.Key;
    }

    public class KeyedService(string key)
    {
        public string Key => key;
    }
    
    public class AbcService : ServiceAttribute
    {
        public AbcService() : base("abc")
        {
        }
    }
#endif
}
