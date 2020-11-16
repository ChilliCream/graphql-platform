using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.HelloWorldSchemaFirst
{
    public class HelloWorldSchemaFirstTests
    {
        [Fact]
        public async Task SimpleHelloWorldWithoutTypeBinding()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello }",
                configure: c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello: String
                        }")
                    .AddResolver("Query", "hello", () => "world"))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithArgumentWithoutTypeBinding()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello(a: \"foo\") }",
                configure: c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello(a: String!): String
                        }")
                    .AddResolver("Query", "hello", ctx => ctx.ArgumentValue<string>("a")))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithResolverType()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello world }",
                configure: c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello: String
                            world: String
                        }")
                    .BindResolver<QueryA>(c => c.To("Query").Resolve("hello").With(t => t.Hello))
                    .BindResolver<QueryB>(c => c.To("Query").Resolve("world").With(t => t.World)))
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task SimpleHelloWorldWithResolverTypeAndArgument()
        {
            Snapshot.FullName();
            await ExpectValid(
                "{ hello(a: \"foo_\") }",
                configure: c => c
                    .AddDocumentFromString(
                        @"
                        type Query {
                            hello(a: String!): String
                        }")
                    .BindResolver<QueryA>(c => c
                        .To("Query")
                        .Resolve("hello")
                        .With(t => t.GetHello(default, default))))
                .MatchSnapshotAsync();
        }

        public class QueryA
        {
            public string Hello => "World";

            public string GetHello(string a, IResolverContext context)
            {
                return a + context.ArgumentValue<string>("a");
            }
        }

        public class QueryB
        {
            public string World => "Hello";
        }
    }
}
