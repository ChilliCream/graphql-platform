using System.Threading.Tasks;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Execution;

public class SchemaFirstTests
{
    [Fact]
    public async Task BindObjectTypeImplicit()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        test: String
                        testProp: String
                    }")
            .AddResolver<Query>()
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ test testProp }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task BindInputTypeImplicit()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ foo(bar: { baz: \"hello\"}) }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task EnumAsOutputType()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ enumValue }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task EnumAsInputType()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Query {
                        setEnumValue(value:FooEnum) : String
                    }

                    enum FooEnum {
                        BAR
                        BAZ_BAR
                    }")
            .AddResolver<EnumQuery>("Query")
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ setEnumValue(value:BAZ_BAR) }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InputObjectWithEnum()
    {
        // arrange
        var schema = SchemaBuilder.New()
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
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ enumInInputObject(payload: { value:BAZ } ) }");

        // assert
        Assert.Null(Assert.IsType<QueryResult>(result).Errors);
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
        Baz,
        BazBar
    }
}