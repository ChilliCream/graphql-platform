using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task BindObjectTypeImplicit()
        {
            // arrange
            var schema = Schema.Create(
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task BindInputTypeImplicit()
        {
            // arrange
            var schema = Schema.Create(
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
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EnumAsOutputType()
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    enumValue: FooEnum
                }

                enum FooEnum {
                    BAR
                    BAZ
                }
                ",

                c => c.BindType<EnumQuery>().To("Query"));

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ enumValue }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EnumAsInputType()
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    setEnumValue(value:FooEnum) : String
                }

                enum FooEnum {
                    BAR
                    BAZ
                }
                ",

                c => c.BindType<EnumQuery>().To("Query"));

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ setEnumValue(value:BAZ) }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InputObjectWithEnum()
        {
            // arrange
            var schema = Schema.Create(
                @"
                type Query {
                    enumInInputObject(payload:Payload) : String
                }

                input Payload {
                    value: FooEnum
                }

                enum FooEnum {
                    BAR
                    BAZ
                }
                ",
                c =>
                {
                    c.BindType<EnumQuery>().To("Query");
                    c.BindType<Payload>();
                });

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(
                    "{ enumInInputObject(payload: { value:BAZ } ) }");

            // assert
            Assert.Null(result.Errors);
            result.MatchSnapshot();
        }

        public class Query
        {
            public string GetTest()
            {
                return "Hello World 1!";
            }

            public string TestProp => "Hello World 2!";
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

        public class EnumQuery
        {
            public FooEnum GetEnumValue()
            {
                return FooEnum.Bar;
            }

            public string SetEnumValue(FooEnum value)
            {
                return value.ToString();
            }

            public string EnumInInputObject(Payload payload)
            {
                return payload.Value.ToString();
            }
        }

        public class Payload
        {
            public FooEnum Value { get; set; }
        }

        public enum FooEnum
        {
            Bar,
            Baz
        }
    }
}
