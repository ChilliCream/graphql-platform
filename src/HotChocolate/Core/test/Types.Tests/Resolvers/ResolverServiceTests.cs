using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Snapshooter.Xunit;
using Xunit;

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

        IRequestExecutor executor =
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

        IRequestExecutor executor =
            await new ServiceCollection()
                .AddSingleton<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryResolverService>()
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
}
