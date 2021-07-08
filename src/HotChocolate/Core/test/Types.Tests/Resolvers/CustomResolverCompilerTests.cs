using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        [Fact]
        public void AddParameterEnsureBuilderIsNotNull()
        {
            void Configure()
                => default(IResolverCompilerBuilder)!
                    .AddParameter(ctx => ctx.Document);

            Assert.Throws<ArgumentNullException>(Configure);
        }

        [Fact]
        public void AddParameterEnsureExpressionIsNotNull()
        {
            var mock = new Mock<IResolverCompilerBuilder>();

            void Configure()
                => mock.Object.AddParameter<string>(null!);

            Assert.Throws<ArgumentNullException>(Configure);
        }

        [Fact]
        public void AddServiceEnsureBuilderIsNotNull()
        {
            void Configure()
                => default(IResolverCompilerBuilder)!
                    .AddService<SayHelloService>();

            Assert.Throws<ArgumentNullException>(Configure);
        }

        [Fact]
        public void EnsureRequestExecutorBuilderIsNotNull()
        {
            void Configure()
                => default(IRequestExecutorBuilder)!.ConfigureResolverCompiler(_ => { });

            Assert.Throws<ArgumentNullException>(Configure);
        }

        [Fact]
        public void EnsureConfigureIsNotNull()
        {
            var mock = new Mock<IRequestExecutorBuilder>();

            void Configure() => mock.Object.ConfigureResolverCompiler(null!);

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
    }
}
