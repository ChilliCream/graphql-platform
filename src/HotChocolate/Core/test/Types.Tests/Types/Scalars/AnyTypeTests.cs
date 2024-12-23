using System.Collections.Immutable;
using System.Dynamic;
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

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => foo))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_ObjectCyclic()
    {
        // arrange
        var fooCyclic = new FooCyclic();
        var barCyclic = new BarCyclic();
        fooCyclic.BarCyclic = barCyclic;
        barCyclic.FooCyclic = fooCyclic;

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("fooCyclic")
                    .Type<AnyType>()
                    .Resolve(_ => fooCyclic))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = (await executor.ExecuteAsync("{ fooCyclic }")).ExpectOperationResult();

        // assert
        Assert.Equal("Cycle in object graph detected.", result.Errors?.Single().Exception?.Message);
    }

    [Fact]
    public async Task Output_Return_List()
    {
        // arrange
        var foo = new Foo();
        var bar = new Bar();
        foo.Bar1 = bar;
        foo.Bar2 = bar;

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => new List<Foo> { foo, foo }))
            .Create();

        var executor = schema.MakeExecutable();

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

        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("fooCyclic")
                    .Type<AnyType>()
                    .Resolve(_ => new List<FooCyclic> { fooCyclic, fooCyclic }))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = (await executor.ExecuteAsync("{ fooCyclic }")).ExpectOperationResult();

        // assert
        Assert.Equal("Cycle in object graph detected.", result.Errors?.Single().Exception?.Message);
    }

    [Fact]
    public async Task Output_Return_RecordList()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => new List<FooRecord> { new(), new() }))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_DateTime()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(
                        _ => new DateTimeOffset(
                            new DateTime(2016, 01, 01),
                            TimeSpan.Zero)))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_String()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => "abc"))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Int()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => 123))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Float()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => 1.2))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_Boolean()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Resolve(_ => true))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<Foo>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

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
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", new List<object> { "abc", } }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object_List_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object>
                    {
                        {
                            "foo", new List<object>
                            {
                                new Dictionary<string, object>
                                {
                                    { "abc", "def" },
                                },
                            }
                        },
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_String_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", "bar" }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Int_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", 123 }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Float_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", 1.2 }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Object_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentLiteral<ObjectValueNode>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", new { a = "b", } }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_ObjectDict_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentLiteral<ObjectValueNode>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object>
                    {
                        { "foo", new Dictionary<string, object> { { "a", "b" }, } },
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_ArgumentKind()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentKind("input").ToString()))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(
                    new Dictionary<string, object>
                    {
                        { "foo", new Dictionary<string, object> { { "a", "b" }, } },
                    })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Boolean_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", false }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Null_As_Variable()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("query ($foo: Any) { foo(input: $foo) }")
                .SetVariableValues(new Dictionary<string, object> { { "foo", null }, })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void IsInstanceOfType_EnumValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new EnumValueNode("foo"));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_ObjectValue_True()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new ObjectValueNode([]));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_ListValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new ListValueNode([]));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_StringValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new StringValueNode("foo"));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_IntValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new IntValueNode(123));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_FloatValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new FloatValueNode(1.2));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_BooleanValue_False()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(new BooleanValueNode(true));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullValue_True()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var result = type.IsInstanceOfType(NullValueNode.Default);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_ArgumentNullException()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        void Action() => type.IsInstanceOfType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [InlineData("abc", typeof(StringValueNode))]
    [InlineData((short)1, typeof(IntValueNode))]
    [InlineData((int)1, typeof(IntValueNode))]
    [InlineData((long)1, typeof(IntValueNode))]
    [InlineData((float)1, typeof(FloatValueNode))]
    [InlineData((double)1, typeof(FloatValueNode))]
    [InlineData(true, typeof(BooleanValueNode))]
    [InlineData(false, typeof(BooleanValueNode))]
    [Theory]
    public void ParseValue_ScalarValues(object value, Type expectedType)
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(value);

        // assert
        Assert.IsType(expectedType, literal);
    }

    [Fact]
    public void ParseValue_Decimal()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue((decimal)1);

        // assert
        Assert.IsType<FloatValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_Object()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(new List<object>());

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_String()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(new List<string>());

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_Foo()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");
        var foo = new Foo();
        var bar = new Bar();
        foo.Bar1 = bar;
        foo.Bar2 = bar;

        // act
        var literal = type.ParseValue(new List<Foo> { foo, foo });

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_FooCyclic()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");
        var fooCyclic = new FooCyclic();
        var barCyclic = new BarCyclic();
        fooCyclic.BarCyclic = barCyclic;
        barCyclic.FooCyclic = fooCyclic;

        // act
        void Act() => type.ParseValue(new List<FooCyclic> { fooCyclic, fooCyclic });

        // assert
        Assert.Equal(
            "Cycle in object graph detected.",
            Assert.Throws<GraphQLException>(Act).Message);
    }

    [Fact]
    public void ParseValue_List_Of_FooRecord()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(new List<FooRecord> { new(), new() });

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_Foo()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(new Foo());

        // assert
        Assert.IsType<ObjectValueNode>(literal);
    }

    [Fact]
    public void ParseValue_FooCyclic()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");
        var fooCyclic = new FooCyclic();
        var barCyclic = new BarCyclic();
        fooCyclic.BarCyclic = barCyclic;
        barCyclic.FooCyclic = fooCyclic;

        // act
        void Act() => type.ParseValue(fooCyclic);

        // assert
        Assert.Equal(
            "Cycle in object graph detected.",
            Assert.Throws<GraphQLException>(Act).Message);
    }

    [Fact]
    public void ParseValue_Dictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var literal = type.ParseValue(
            new Dictionary<string, object>());

        // assert
        Assert.IsType<ObjectValueNode>(literal);
    }

    [Fact]
    public void Deserialize_ValueNode()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        // act
        var value = type.Deserialize(new StringValueNode("Foo"));

        // assert
        Assert.Equal("Foo", value);
    }

    [Fact]
    public void Deserialize_Dictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        var toDeserialize = new Dictionary<string, object>
        {
            { "Foo", new StringValueNode("Bar") },
        };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        Assert.Equal("Bar", Assert.IsType<Dictionary<string, object>>(value)["Foo"]);
    }

    [Fact]
    public void Deserialize_NestedDictionary()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");

        var toDeserialize = new Dictionary<string, object>
        {
            { "Foo", new Dictionary<string, object> { { "Bar", new StringValueNode("Baz") }, } },
        };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        var innerDictionary = Assert.IsType<Dictionary<string, object>>(value)["Foo"];
        Assert.Equal("Baz", Assert.IsType<Dictionary<string, object>>(innerDictionary)["Bar"]);
    }

    [Fact]
    public void Deserialize_List()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("foo")
                    .Type<AnyType>()
                    .Argument("input", a => a.Type<AnyType>())
                    .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        var type = schema.GetType<AnyType>("Any");
        var toDeserialize =
            new List<object> { new StringValueNode("Foo"), new StringValueNode("Bar"), };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        Assert.Collection(
            Assert.IsType<object[]>(value)!,
            x => Assert.Equal("Foo", x),
            x => Assert.Equal("Bar", x));
    }

    [Fact]
    public async Task Dictionary_Is_Handled_As_Object()
    {
        await ExpectValid(
                "{ someObject }",
                configure: c => c.AddQueryType<QueryWithDictionary>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UseExpandoObjectWithAny()
    {
        await ExpectValid(
                "{ something }",
                configure: c => c.AddQueryType<SomeQuery>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task UseImmutableDictWithAny()
    {
        await ExpectValid(
                "{ somethingImmutable }",
                configure: c => c.AddQueryType<SomeQuery>())
            .MatchSnapshotAsync();
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
        public BarCyclic BarCyclic { get; set; }
    }

    public class BarCyclic
    {
        public FooCyclic FooCyclic { get; set; }
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
        [GraphQLType(typeof(AnyType))]
        public IDictionary<string, object> SomeObject =>
            new Dictionary<string, object> { { "a", "b" }, };
    }
}
