using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Xunit;

namespace HotChocolate.Integration.HelloWorldSchemaFirst
{
    public class HelloWorldSchemaFirstTests
    {
        [Fact]
        public void SimpleHelloWorldWithoutTypeBinding()
        {
            // arrange
            var schema = Schema.Create(
                @"
                    type Query {
                        hello: String
                    }
                ",
                c =>
                {
                    c.BindResolver(() => "world")
                        .To("Query", "hello");
                });

            // act
            IExecutionResult result = schema.Execute("{ hello }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public void SimpleHelloWorldWithArgumentWithoutTypeBinding()
        {
            // arrange
            var schema = Schema.Create(
                @"
                    type Query {
                        hello(a: String!): String
                    }
                ",
                c =>
                {
                    c.BindResolver(ctx => ctx.Argument<string>("a"))
                        .To("Query", "hello");
                });

            // act
            IExecutionResult result = schema.Execute("{ hello(a: \"foo\") }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public void SimpleHelloWorldWithResolverType()
        {
            // arrange
            var schema = Schema.Create(
                @"
                    type Query {
                        hello: String
                        world: String
                    }
                ",
                c =>
                {
                    c.BindResolver<QueryA>().To("Query")
                        .Resolve("hello").With(t => t.Hello);
                    c.BindResolver<QueryB>().To("Query")
                        .Resolve("world").With(t => t.World);
                });

            // act
            IExecutionResult result = schema.Execute("{ hello world }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public void SimpleHelloWorldWithResolverTypeAndArgument()
        {
            // arrange
            var schema = Schema.Create(
                @"
                    type Query {
                        hello(a: String!): String
                    }
                ",
                c =>
                {
                    c.BindResolver<QueryA>().To("Query")
                        .Resolve("hello")
                        .With(t => t.GetHello(default, default));
                });

            // act
            IExecutionResult result = schema.Execute("{ hello(a: \"foo_\") }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        public class QueryA
        {
            public string Hello => "World";

            public string GetHello(string a, IResolverContext context)
            {
                return a + context.Argument<string>("a");
            }
        }

        public class QueryB
        {
            public string World => "Hello";
        }
    }
}
