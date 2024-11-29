using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;

#nullable enable

namespace HotChocolate.Resolvers;

public class CustomResolverCompilerTests
{
    [Fact]
    public async Task AddWellKnownState_New()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWellKnownState>()
            .AddParameterExpressionBuilder(ctx => (SayHelloState)ctx.ContextData["someState"]!)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument("{ sayHello }")
                    .AddGlobalState("someState", new SayHelloState("Hello"))
                    .Build())
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

    public class SayHelloState(string greetings)
    {
        public string Greetings { get; } = greetings;
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
