using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#nullable enable

namespace HotChocolate.Resolvers
{
    public class CustomResolverCompilerTests
    {
        [Fact]
        public async Task AddWellKnownService()
        {
            await new ServiceCollection()
                .AddSingleton<SayHelloService>()
                .AddGraphQL()
                .AddQueryType<QueryWellKnownService>()
                .ConfigureResolverCompiler(c =>
                {
                    c.AddService<SayHelloService>();
                })
                .ExecuteRequestAsync("{ sayHello }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task AddWellKnownState()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWellKnownState>()
                .ConfigureResolverCompiler(c =>
                {
                    c.AddParameter(ctx => (SayHelloState)ctx.ContextData["someState"]!);
                })
                .ExecuteRequestAsync(
                    QueryRequestBuilder.New()
                        .SetQuery("{ sayHello }")
                        .AddProperty("someState", new SayHelloState("Hello"))
                        .Create())
                .MatchSnapshotAsync();
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
    }
}
