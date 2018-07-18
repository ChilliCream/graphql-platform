using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Integration.HelloWorldSchemaFirst
{
    public class HelloWorldSchemaFirstTests
    {
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
