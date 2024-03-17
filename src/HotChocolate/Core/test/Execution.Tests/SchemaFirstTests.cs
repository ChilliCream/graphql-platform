#nullable enable
using CookieCrumble;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;

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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
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
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InputObjectWithEnum()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                  enumInInputObject(payload: Payload) : String
                }

                input Payload {
                  value: FooEnum
                }

                enum FooEnum {
                  BAR
                  BAZ
                }
                """)
            .AddResolver<EnumQuery>("Query")
            .AddResolver<Payload>()
            .Create();

        // act
        var result =
            await schema.MakeExecutable().ExecuteAsync(
                "{ enumInInputObject(payload: { value:BAZ } ) }");

        // assert
        Assert.Null(Assert.IsType<OperationResult>(result).Errors);
        result.MatchSnapshot();
    }

    /// <summary>
    /// https://github.com/ChilliCream/graphql-platform/issues/5730
    /// </summary>
    [Fact]
    public async Task Issue_5730()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    schema {
                      query: Query
                      mutation: Mutation
                    }

                    type Query {
                       dummy: String!
                    }

                    type Mutation {
                        changeChannelParameters(
                            input: ChangeChannelParameterInput!)
                            : ChangeChannelParameterPayload!
                    }

                    input ChangeChannelParameterInput {
                      parameterChangeInfo: [ParameterValuePair!]!
                    }

                    input ParameterValuePair {
                      value: Any
                    }

                    type ChangeChannelParameterPayload {
                      message: String!
                    }

                    scalar Any
                    """)
                .AddResolver<Query5730>("Query")
                .AddResolver<Mutation5730>("Mutation")
                .ExecuteRequestAsync(
                    """
                    mutation {
                      changeChannelParameters(input: {
                        parameterChangeInfo: [ { value: { a: "b" } } ]
                      }) {
                        message
                      }
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "changeChannelParameters": {
                  "message": "b"
                }
              }
            }
            """);
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
        public string Baz { get; set; } = default!;
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
        BazBar,
    }

    public class Query5730
    {
        public string Dummy => "Don't care";
    }

    public class Mutation5730
    {
        public Task<ChangeChannelParameterPayload> ChangeChannelParametersAsync(
            ChangeChannelParameterInput input,
            CancellationToken _)
        {
            var message = Assert.IsType<string>(
                Assert.IsType<Dictionary<string, object>>(
                    input.ParameterChangeInfo[0].Value)["a"]);

            return Task.FromResult(new ChangeChannelParameterPayload { Message = message, });
        }
    }

    public record ChangeChannelParameterInput
    {
        public ParameterValuePair[] ParameterChangeInfo { get; set; } =
            Array.Empty<ParameterValuePair>();
    }

    public record ParameterValuePair
    {
        public object? Value { get; set; }
    }

    public record ChangeChannelParameterPayload
    {
        public string Message { get; init; } = string.Empty;
    }
}
