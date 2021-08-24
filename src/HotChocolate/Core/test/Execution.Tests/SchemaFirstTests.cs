using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task BindObjectTypeImplicit()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        test: String
                        testProp: String
                    }")
                .AddResolver<Query>()
                .Create();

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
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"schema {
                    query: FooQuery
                }

                type FooQuery {
                    foo(bar: Bar): String
                }

                input Bar
                {
                    baz: String
                }")
                .AddResolver<FooQuery>()
                .AddResolver<Bar>()
                .Create();

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
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        enumValue: FooEnum
                    }

                    enum FooEnum {
                        BAR
                        BAZ
                    }")
                .AddResolver<EnumQuery>("Query")
                .Create();

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
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        setEnumValue(value:FooEnum) : String
                    }

                    enum FooEnum {
                        BAR
                        BAZ
                    }")
                .AddResolver<EnumQuery>("Query")
                .Create();

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
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        enumInInputObject(payload:Payload) : String
                    }

                    input Payload {
                        value: FooEnum
                    }

                    enum FooEnum {
                        BAR
                        BAZ
                    }")
                .AddResolver<EnumQuery>("Query")
                .AddResolver<Payload>()
                .Create();

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
