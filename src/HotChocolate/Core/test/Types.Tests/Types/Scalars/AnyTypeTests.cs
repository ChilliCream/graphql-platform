using System.Collections.Immutable;
using System.Dynamic;
using System.Numerics;
using System.Text.Json;
using CookieCrumble.Xunit.Attributes;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Types;

public class AnyTypeTests
{
    [Fact]
    public async Task Output_Return_Object()
    {
        // arrange
        var foo = new Foo();
        var bar = new Bar();
        foo.Bar1 = bar;
        foo.Bar2 = bar;

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => foo))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_ObjectCyclic()
    {
        // arrange
        var fooCyclic = new FooCyclic();
        var barCyclic = new BarCyclic();
        fooCyclic.BarCyclic = barCyclic;
        barCyclic.FooCyclic = fooCyclic;

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("fooCyclic")
                        .Type<AnyType>()
                        .Resolve(_ => fooCyclic))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = (await executor.ExecuteAsync("{ fooCyclic }")).ExpectOperationResult();

        // assert
        Assert.Equal(
            "Any cannot coerce the runtime value of type `HotChocolate.Types.AnyTypeTests+FooCyclic` "
            + "into the result value format.",
            result.Errors?.Single()?.Message);
    }

    [Fact]
    public async Task Output_Return_List()
    {
        // arrange
        var foo = new Foo();
        var bar = new Bar();
        foo.Bar1 = bar;
        foo.Bar2 = bar;

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => new List<Foo> { foo, foo }))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_ListCyclic()
    {
        // arrange
        var fooCyclic = new FooCyclic();
        var barCyclic = new BarCyclic();
        fooCyclic.BarCyclic = barCyclic;
        barCyclic.FooCyclic = fooCyclic;

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("fooCyclic")
                        .Type<AnyType>()
                        .Resolve(_ => new List<FooCyclic> { fooCyclic, fooCyclic }))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = (await executor.ExecuteAsync("{ fooCyclic }")).ExpectOperationResult();

        // assert
        Assert.Equal(
            "Any cannot coerce the runtime value of type `System.Collections.Generic.List`1"
            + "[[HotChocolate.Types.AnyTypeTests+FooCyclic, HotChocolate.Types.Tests, Version=0.0.0.0, "
            + "Culture=neutral, PublicKeyToken=null]]` into the result value format.",
            result.Errors?.Single()?.Message);
    }

    [Fact]
    public async Task Output_Return_RecordList()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => new List<FooRecord> { new(), new() }))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_DateTime()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(
                            _ => new DateTimeOffset(
                                new DateTime(2016, 01, 01),
                                TimeSpan.Zero)))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "foo": "2016-01-01T00:00:00+00:00"
              }
            }
            """);
    }

    [Fact]
    public async Task Output_Return_String()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => "abc"))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Int()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => 123))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Float()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => 1.2))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Boolean()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Resolve(_ => true))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: { a: \"foo\" }) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_List()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: [ \"foo\" ]) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object_List()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: [ { a: \"foo\" } ]) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Object_To_Foo()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<Foo>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: { bar: { baz: \"FooBar\" } }) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_String()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: \"foo\") }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Int()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: 123) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Float()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: 1.2) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Boolean()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: true) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Null()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(input: null) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_List_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", new List<object> { "abc" } } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object_List_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        {
                            "foo", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    { "abc", "def" }
                                }
                            }
                        }
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_String_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", "bar" } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Int_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", 123 } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Float_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", 1.2 } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Object_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentLiteral<ObjectValueNode>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", new { a = "b" } } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_ObjectDict_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentLiteral<ObjectValueNode>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "foo", new Dictionary<string, object> { { "a", "b" } } }
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_ArgumentKind()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentKind("input").ToString()))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "foo", new Dictionary<string, object> { { "a", "b" } } }
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Boolean_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", false } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Null_As_Variable()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object?> { { "foo", null } })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task IsValueCompatible_EnumValue_False()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new EnumValueNode("foo"));

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsValueCompatible_ObjectValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new ObjectValueNode([]));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_ListValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new ListValueNode([]));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_StringValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new StringValueNode("foo"));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_IntValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new IntValueNode(123));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_FloatValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new FloatValueNode(1.2));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_BooleanValue_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(new BooleanValueNode(true));

        // assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsValueCompatible_NullValue_False()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(NullValueNode.Default);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsValueCompatible_Null_ReturnsFalse()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var result = type.IsValueCompatible(null!);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValueToLiteral_String()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement("abc");

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<StringValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_Int()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(123);

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<IntValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_Float()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(1.5);

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<FloatValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_True()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(true);

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<BooleanValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_False()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(false);

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<BooleanValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_Decimal()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(1.0m);

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<FloatValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_List_Of_Object()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(new List<object>());

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_List_Of_String()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(new List<string>());

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_List_Of_Foo()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var foo = new Foo();
        var bar = new Bar();
        foo.Bar1 = bar;
        foo.Bar2 = bar;
        var value = JsonSerializer.SerializeToElement(new List<Foo> { foo, foo });

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_List_Of_FooRecord()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(new List<FooRecord> { new(), new() });

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_Foo()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(new Foo());

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ObjectValueNode>(literal);
    }

    [Fact]
    public async Task ValueToLiteral_Dictionary()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");
        var value = JsonSerializer.SerializeToElement(new Dictionary<string, object>());

        // act
        var literal = type.ValueToLiteral(value);

        // assert
        Assert.IsType<ObjectValueNode>(literal);
    }

    [Fact]
    public async Task CoerceInputLiteral_StringValueNode()
    {
        // arrange
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(
                    d => d
                        .Name("Query")
                        .Field("foo")
                        .Type<AnyType>()
                        .Argument("input", a => a.Type<AnyType>())
                        .Resolve(ctx => ctx.ArgumentValue<object>("input")))
                .AddJsonTypeConverter()
                .BuildSchemaAsync();

        var type = schema.Types.GetType<AnyType>("Any");

        // act
        var value = type.CoerceInputLiteral(new StringValueNode("Foo"));

        // assert
        Assert.Equal("Foo", Assert.IsType<JsonElement>(value).GetString());
    }

    [Fact]
    public async Task Dictionary_Is_Handled_As_Object()
    {
        await ExpectValid(
            """
            {
              someObject
            }
            """,
            configure: c => c
                .AddQueryType<QueryWithDictionary>()
                .AddJsonTypeConverter())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UseExpandoObjectWithAny()
    {
        await ExpectValid(
                "{ something }",
                configure: c => c
                    .AddQueryType<SomeQuery>()
                    .AddJsonTypeConverter())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UseImmutableDictWithAny()
    {
        await ExpectValid(
                "{ somethingImmutable }",
                configure: c => c
                    .AddQueryType<SomeQuery>()
                    .AddJsonTypeConverter())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task JsonElement_Schema()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: QueryJsonElement
            }

            type QueryJsonElement {
              someJson: Any!
              manyJson: [Any!]!
              inputJson(input: Any!): Any!
              jsonFromString: Any!
            }

            "The `@specifiedBy` directive is used within the type system definition language to provide a URL for specifying the behavior of custom scalar definitions."
            directive @specifiedBy("The specifiedBy URL points to a human-readable specification. This field will only read a result for scalar types." url: String!) on SCALAR

            "The `Any` scalar type represents any valid GraphQL value."
            scalar Any @specifiedBy(url: "https://scalars.graphql.org/chillicream/any.html")
            """);
    }

    [Fact]
    public async Task JsonElement_Output_Json_Object()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        someJson
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "someJson": {
                  "a": {
                    "b": 123.456
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Output_Json_Object_List()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        manyJson
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "manyJson": [
                  {
                    "a": {
                      "b": 123.456
                    }
                  },
                  {
                    "x": {
                      "y": "y"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Input_Json_Object_Literal()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        inputJson(input: { a: "abc" })
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "inputJson": {
                  "a": "abc"
                }
              }
            }
            """);
    }

    [Theory]
    [UseCulture("en-US")]
    [InlineData(0)]
    [InlineData(-15)]
    [InlineData(-10.5)]
    [InlineData(1.5)]
    [InlineData(1e15)]
    public async Task JsonElement_Input_Json_Number_Literal(decimal value)
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    $$"""
                    {
                        inputJson(input: {{value}})
                    }
                    """);

        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "inputJson": {{value}}
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Input_Json_BigInt_Literal()
    {
        var value = BigInteger.Parse("100000000000000000000000050");

        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    $$"""
                    {
                        inputJson(input: {{value}})
                    }
                    """);

        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "inputJson": {{value}}
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Input_Json_Exponent_Literal()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        inputJson(input: 1e1345)
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "inputJson": 1e1345
              }
            }
            """);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public async Task JsonElement_Input_Json_Bool_Literal(string value)
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    $$"""
                    {
                        inputJson(input: {{value}})
                    }
                    """);

        result.MatchInlineSnapshot(
            $$"""
            {
              "data": {
                "inputJson": {{value}}
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Input_Json_Object_List()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        inputJson(input: { a: ["abc"] })
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "inputJson": {
                  "a": [
                    "abc"
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Input_Json_Object_Variables()
    {
        var input = JsonDocument.Parse(
            """
            {
              "a": {
                "b": 123.456
              }
            }
            """).RootElement;

        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    OperationRequestBuilder.New()
                        .SetDocument(
                            """
                            query($input: Any!) {
                                inputJson(input: $input)
                            }
                            """)
                        .SetVariableValues(new Dictionary<string, object?> { { "input", input } })
                        .Build());

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "inputJson": {
                  "a": {
                    "b": 123.456
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task JsonElement_Output_Json_From_String()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<QueryJsonElement>()
                .ExecuteRequestAsync(
                    """
                    {
                        jsonFromString
                    }
                    """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "jsonFromString": {
                  "a": "b"
                }
              }
            }
            """);
    }

    public class SomeQuery
    {
        [GraphQLType<AnyType>]
        public object GetSomething()
        {
            dynamic obj = new ExpandoObject();
            obj.a = "Foo";
            return obj;
        }

        [GraphQLType<AnyType>]
        public ImmutableDictionary<string, object> GetSomethingImmutable()
        {
            return ImmutableDictionary<string, object>.Empty.Add("a", "Foo");
        }
    }

    public class Foo
    {
        public Bar Bar1 { get; set; } = new Bar();

        public Bar Bar2 { get; set; } = new Bar();
    }

    public class Bar
    {
        public string Baz { get; set; } = "Baz";
    }

    public class FooCyclic
    {
        public BarCyclic? BarCyclic { get; set; }
    }

    public class BarCyclic
    {
        public FooCyclic? FooCyclic { get; set; }
    }

    public record FooRecord
    {
        public BarRecord BarRecord { get; set; } = new BarRecord();
    }

    public record BarRecord
    {
        public string Baz { get; set; } = "Baz";
    }

    public class QueryWithDictionary
    {
        [GraphQLType<AnyType>]
        public Dictionary<string, object> SomeObject
            => new Dictionary<string, object> { { "a", "b" } };
    }

    public class QueryJsonElement
    {
        public JsonElement GetSomeJson()
            => JsonDocument.Parse(
                """
                {
                  "a": {
                    "b": 123.456
                  }
                }
                """).RootElement;

        public IEnumerable<JsonElement> GetManyJson()
        {
            yield return JsonDocument.Parse(
                """
                {
                  "a": {
                    "b": 123.456
                  }
                }
                """).RootElement;

            yield return JsonDocument.Parse(
                """
                {
                  "x": {
                    "y": "y"
                  }
                }
                """).RootElement;
        }

        public JsonElement InputJson(JsonElement input)
            => input;

        [GraphQLType<NonNullType<AnyType>>]
        public string JsonFromString()
            => "{ \"a\": \"b\" }";
    }
}
