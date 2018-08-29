using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Integration.HelloWorldSchemaFirst
{
    public class HelloWorldSchemaFirstTests
    {
        [Fact]
        public void SimpleHelloWorldWithoutTypeBinding()
        {
            // arrange
            Schema schema = Schema.Create(
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void SimpleHelloWorldWithArgumentWithoutTypeBinding()
        {
            // arrange
            Schema schema = Schema.Create(
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        [Fact]
        public void Foo()
        {
            // arrange
            Schema schema = CreateSchema();

            // act
            IExecutionResult result = schema.Execute("{ hello world }");

            // assert
            Assert.Null(result.Errors);
            Assert.Equal(Snapshot.Current(), Snapshot.New(result));
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(
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
        }

        public class QueryA
        {
            public string Hello => "World";
        }

        public class QueryB
        {
            public string World => "Hello";
        }
    }
}
