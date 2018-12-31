using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task ExecuteOneFieldQuery()
        {
            // arrange
            Schema schema = Schema.Create(
                @"
                type Query {
                    test: String
                    testProp: String
                }",
                c => c.BindType<Query>());

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ test testProp }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task BindInputType()
        {
            // arrange
            Schema schema = Schema.Create(
                @"
                schema {
                    query: FooQuery
                }

                type FooQuery {
                    foo(bar: Bar): String
                }

                input Bar
                {
                    baz: String
                }",
                c =>
                {
                    c.BindType<FooQuery>();
                    c.BindType<Bar>();
                });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ foo(bar: { baz: \"hello\"}) }");

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        public class Query
        {
            public string GetTest()
            {
                return "Hello World!";
            }

            public string TestProp => "Hello World!";
        }

        public class FooQuery
        {
            public string GetFoo(Bar bar)
            {
                return bar.Baz;
            }
        }

        public class Bar
        {
            public string Baz { get; set; }
        }
    }
}
