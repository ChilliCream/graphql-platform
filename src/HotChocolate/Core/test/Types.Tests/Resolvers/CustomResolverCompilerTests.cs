using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using Snapshooter.Xunit;

#nullable enable

namespace HotChocolate.Resolvers;

public class CustomResolverCompilerTests
{
    [Fact]
    public async Task AddDefaultService()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddSingleton<SayHelloService>()
            .AddGraphQL()
            .AddQueryType<QueryWellKnownService>()
            .RegisterService<SayHelloService>()
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
            .AddQueryType<QueryWellKnownService>()
            .RegisterService<SayHelloService>(ServiceKind.Pooled)
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
                .AddQueryType<QueryWellKnownService>()
                .RegisterService<SayHelloService>(ServiceKind.Synchronized)
                .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
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
                .AddQueryType<QueryWellKnownService>()
                .RegisterService<SayHelloService>(ServiceKind.Resolver)
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
    public async Task AddWellKnownState_New()
    {
        Snapshot.FullName();

        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWellKnownState>()
            .AddParameterExpressionBuilder(ctx => (SayHelloState)ctx.ContextData["someState"]!)
            .ExecuteRequestAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ sayHello }")
                    .AddGlobalState("someState", new SayHelloState("Hello"))
                    .Create())
            .MatchSnapshotAsync();
    }

    [Fact]
    public void AddParameterEnsureBuilderIsNotNull_New()
    {
        void Configure()
            => default(IRequestExecutorBuilder)!
                .AddParameterExpressionBuilder(ctx => ctx.Operation.Document);

        Assert.Throws<ArgumentNullException>(Configure);
    }

    public class SayHelloService
    {
        public string SayHello() => "Hello";
    }

    public class QueryWellKnownService
    {
        public string SayHello(SayHelloService service)
            => service.SayHello();
    }

    public class SayHelloState
    {
        public SayHelloState(string greetings)
        {
            Greetings = greetings;
        }

        public string Greetings { get; }
    }

    public class QueryWellKnownState
    {
        public string SayHello(SayHelloState state)
            => state.Greetings;
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
