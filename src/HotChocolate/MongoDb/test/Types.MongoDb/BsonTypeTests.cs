using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using BsonType = HotChocolate.Types.MongoDb.BsonType;

namespace HotChocolate.Types;

public class BsonTypeTests
{
    [Fact]
    public async Task Output_Should_BindAllRuntimeTypes()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType<OutputQuery>()
            .BuildRequestExecutorAsync();

        // act
        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Should_MatchSnapshot_When_BsonDocument()
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType<OutputQuery>()
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync("{ document }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Should_MatchSnapshot_When_BsonDocument()
    {
        // arrange
        object res = "INVALID";
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType(x => x.Name("Query")
                .Field("in")
                .Type<StringType>()
                .Argument("val", x => x.Type<BsonType>())
                .Resolve(ctx =>
                {
                    res = ctx.ArgumentValue<object>("val");
                    return "done";
                }))
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(@"
        {
            in(val: {
                  int32: 42,
                  int64: 42,
                  decimal: ""42.123456789123456789123456789"",
                  double: 42.23,
                  boolean: true,
                  bsonArray: [
                    false,
                    true
                  ],
                  string: ""String"",
                  null: null,
                  nested: {
                    int32: 42,
                    int64: 42
                  }
                })
            }
        ");

        // assert
        Assert.IsType<BsonDocument>(res).ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Should_MatchSnapshotAndType_When_DictionaryPassed()
    {
        // arrange
        object res = "INVALID";
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType(x => x.Name("Query")
                .Field("in")
                .Type<StringType>()
                .Argument("val", x => x.Type<BsonType>())
                .Resolve(ctx =>
                {
                    res = ctx.ArgumentValue<object>("val");
                    return "done";
                }))
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync("query Test($val: Bson){ in(val:$val) }",
            new Dictionary<string, object?>()
            {
                ["val"] = new Dictionary<string, object>() { ["foo"] = true }
            });

        // assert
        Assert.NotEqual("INVALID", res);
        Assert.IsType<BsonDocument>(res).ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Should_MatchSnapshotAndType_When_ListPassed()
    {
        // arrange
        object res = "INVALID";
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType(x => x.Name("Query")
                .Field("in")
                .Type<StringType>()
                .Argument("val", x => x.Type<BsonType>())
                .Resolve(ctx =>
                {
                    res = ctx.ArgumentValue<object>("val");
                    return "done";
                }))
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync("query Test($val: Bson){ in(val:$val) }",
            new Dictionary<string, object?>() { ["val"] = new List<string>() { "foo", "bar" } });

        // assert
        Assert.NotEqual("INVALID", res);
        Assert.IsType<BsonArray>(res).ToString().MatchSnapshot();
    }

    [Theory]
    [InlineData("int32")]
    [InlineData("int64")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("boolean")]
    [InlineData("bsonArray")]
    [InlineData("string")]
    [InlineData("objectId")]
    [InlineData("binary")]
    [InlineData("timestamp")]
    [InlineData("dateTime")]
    [InlineData("string")]
    [InlineData("null")]
    public async Task Output_Should_MatchSnapshot_When_Value(string fieldName)
    {
        // arrange
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType<OutputQuery>()
            .BuildRequestExecutorAsync();

        // act
        IExecutionResult result = await executor.ExecuteAsync($"{{ {fieldName} }}");

        // assert
        result.ToJson().MatchSnapshot(new SnapshotNameExtension(fieldName));
    }

    [Theory]
    [InlineData("int", "42", typeof(BsonInt64))]
    [InlineData("long", long.MaxValue, typeof(BsonInt64))]
    [InlineData("decimal",
        "\"42.1234\"",
        typeof(BsonString))] // we do not know that it should be a BsonDecimal
    [InlineData("double", 43.23, typeof(BsonDouble))]
    [InlineData("boolean", true, typeof(BsonBoolean))]
    [InlineData("array", "[true, false]", typeof(BsonArray))]
    [InlineData("string", "\"string\"", typeof(BsonString))]
    public async Task Input_Should_MatchSnapshotAndType_When_Passed(
        string fieldName,
        object value,
        Type type)
    {
        // arrange
        object res = "INVALID";
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType(x => x.Name("Query")
                .Field("in")
                .Type<StringType>()
                .Argument("val", x => x.Type<BsonType>())
                .Resolve(ctx =>
                {
                    res = ctx.ArgumentValue<object>("val");
                    return "done";
                }))
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync($"{{ in(val:{value.ToString()!.ToLower()}) }}");

        // assert
        Assert.NotEqual("INVALID", res);
        Assert.IsType(type, res);
        res.MatchSnapshot(new SnapshotNameExtension(fieldName));
    }

    [Theory]
    [InlineData("int", 42, typeof(BsonInt64))]
    [InlineData("long", long.MaxValue, typeof(BsonInt64))]
    [InlineData("double", 43.23, typeof(BsonDouble))]
    [InlineData("boolean", true, typeof(BsonBoolean))]
    [InlineData("string", "string", typeof(BsonString))]
    public async Task Input_Should_MatchSnapshotAndType_When_PassedVariable(
        string fieldName,
        object value,
        Type type)
    {
        // arrange
        object res = "INVALID";
        IRequestExecutor executor = await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .AddQueryType(x => x.Name("Query")
                .Field("in")
                .Type<StringType>()
                .Argument("val", x => x.Type<BsonType>())
                .Resolve(ctx =>
                {
                    res = ctx.ArgumentValue<object>("val");
                    return "done";
                }))
            .BuildRequestExecutorAsync();

        // act
        await executor.ExecuteAsync("query Test($val: Bson){ in(val:$val) }",
            new Dictionary<string, object?>() { ["val"] = value });

        // assert
        Assert.NotEqual("INVALID", res);
        Assert.IsType(type, res);
        res.MatchSnapshot(new SnapshotNameExtension(fieldName));
    }

    [Fact]
    public async Task TrySerialize_Should_ReturnNull_When_CalledWithNull()
    {
        // arrange
        object res = "INVALID";
        BsonType type = (await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .ModifyOptions(x => x.StrictValidation = false)
            .BuildSchemaAsync()).GetType<BsonType>("Bson");

        // act
        var serialize = type.TrySerialize(null, out var value);

        // assert
        Assert.True(serialize);
        Assert.Null(value);
    }

    [Fact]
    public async Task TrySerialize_Should_ReturnFalse_When_CalledWithNonBsonValue()
    {
        // arrange
        object res = "INVALID";
        BsonType type = (await new ServiceCollection()
            .AddGraphQL()
            .AddBsonType()
            .ModifyOptions(x => x.StrictValidation = false)
            .BuildSchemaAsync()).GetType<BsonType>("Bson");

        // act
        bool result = type.TrySerialize("Failes", out _);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task Output_Return_Object()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Resolve(_ => new BsonDocument() { { "foo", "bar" } }))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Output_Return_List()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Resolve(_ => new BsonArray(){ new BsonDocument() }))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync("{ foo }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: { a: \"foo\" }) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_List()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: [ \"foo\" ]) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object_List()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: [ { a: \"foo\" } ]) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_String()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: \"foo\") }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Int()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: 123) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Float()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: 1.2) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Boolean()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: true) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Null()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            "{ foo(input: null) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_List_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", new List<object> { "abc" })
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Object_List_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo",
                    new List<object> { new Dictionary<string, object> { { "abc", "def" } } })
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_String_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", "bar")
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Int_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", 123)
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Float_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", 1.2)
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_BsonDocument_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentLiteral<ObjectValueNode>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", new BsonDocument { { "a", "b" } })
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Boolean_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", false)
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_Value_Null_As_Variable()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        IRequestExecutor executor = schema.MakeExecutable();

        // act
        IExecutionResult result = await executor.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("query ($foo: Bson) { foo(input: $foo) }")
                .SetVariableValue("foo", null)
                .Create());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void IsInstanceOfType_EnumValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new EnumValueNode("foo"));

        // assert
        Assert.False(result);
    }

    [Fact]
    public void IsInstanceOfType_ObjectValue_True()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new ObjectValueNode(Array.Empty<ObjectFieldNode>()));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_ListValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new ListValueNode(Array.Empty<IValueNode>()));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_StringValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new StringValueNode("foo"));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_IntValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new IntValueNode(123));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_FloatValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new FloatValueNode(1.2));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_BooleanValue_False()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(new BooleanValueNode(true));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_NullValue_True()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var result = type.IsInstanceOfType(NullValueNode.Default);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void IsInstanceOfType_Null_ArgumentNullException()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

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
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue(value);

        // assert
        Assert.IsType(expectedType, literal);
    }

    [Fact]
    public void ParseValue_Decimal()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue((decimal)1);

        // assert
        Assert.IsType<StringValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_Object()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue(new List<object>());

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_String()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue(new List<string>());

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_List_Of_Foo()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue(new List<Foo>());

        // assert
        Assert.IsType<ListValueNode>(literal);
    }

    [Fact]
    public void ParseValue_Dictionary()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        IValueNode literal = type.ParseValue(
            new Dictionary<string, object>());

        // assert
        Assert.IsType<ObjectValueNode>(literal);
    }

    [Fact]
    public void Deserialize_ValueNode()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        // act
        var value = type.Deserialize(new StringValueNode("Foo"));

        // assert
        Assert.Equal("Foo", Assert.IsType<BsonString>(value).Value);
    }

    [Fact]
    public void Deserialize_Dictionary()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        var toDeserialize = new Dictionary<string, object>
        {
            { "Foo", new StringValueNode("Bar") }
        };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        Assert.Equal("Bar", Assert.IsType<BsonDocument>(value)["Foo"]);
    }

    [Fact]
    public void Deserialize_NestedDictionary()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");

        var toDeserialize = new Dictionary<string, object>
        {
            { "Foo", new Dictionary<string, object> { { "Bar", new StringValueNode("Baz") } } }
        };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        var innerDictionary = Assert.IsType<BsonDocument>(value)["Foo"];
        Assert.Equal("Baz", Assert.IsType<BsonDocument>(innerDictionary)["Bar"]);
    }

    [Fact]
    public void Deserialize_List()
    {
        // arrange
        ISchema schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("foo")
                .Type<BsonType>()
                .Argument("input", a => a.Type<BsonType>())
                .Resolve(ctx => ctx.ArgumentValue<object>("input")))
            .Create();

        BsonType type = schema.GetType<BsonType>("Bson");
        var toDeserialize =
            new List<object> { new StringValueNode("Foo"), new StringValueNode("Bar") };

        // act
        var value = type.Deserialize(toDeserialize);

        // assert
        Assert.Collection(
            Assert.IsType<BsonArray>(value)!,
            x => Assert.Equal("Foo", x),
            x => Assert.Equal("Bar", x));
    }

    public class Foo
    {
        public Bar Bar { get; set; } = new Bar();
    }

    public class Bar
    {
        public string Baz { get; set; } = "Baz";
    }

    public class QueryWithDictionary
    {
        [GraphQLType(typeof(BsonType))]
        public IDictionary<string, object> SomeObject =>
            new Dictionary<string, object> { { "a", "b" } };
    }

    public class OutputQuery
    {
        public BsonInt32 Int32 => new(42);

        public BsonInt64 Int64 => new(42);

        public BsonDateTime DateTime => new(1638147536);

        public BsonTimestamp Timestamp => new(1638147536);

        public BsonObjectId ObjectId => new BsonObjectId(new ObjectId("6124e80f3f5fc839830c1f6b"));

        public BsonBinaryData Binary => new BsonBinaryData(new byte[]
        {
            1,
            2,
            3,
            4,
            5,
            6
        });

        public BsonDecimal128 Decimal => new(42.123456789123456789123456789123456789123456789m);

        public BsonDouble Double => new(42.23);

        public BsonBoolean Boolean => new(true);

        public BsonArray BsonArray => new(new[]
        {
            BsonBoolean.False,
            BsonBoolean.True
        });

        public BsonString String => new("String");

        public BsonNull? Null => BsonNull.Value;

        public BsonDocument Document { get; } = new()
        {
            ["Int32"] = new BsonInt32(42),
            ["Int64"] = new BsonInt64(42),
            ["Decimal"] = new BsonDecimal128(42.123456789123456789123456789123456789123456789m),
            ["Double"] = new BsonDouble(42.23),
            ["DateTime"] = new BsonDateTime(1638147536),
            ["Timestamp"] = new BsonTimestamp(1638147536),
            ["ObjectId"] = new BsonObjectId(new ObjectId("6124e80f3f5fc839830c1f6b")),
            ["BinaryData"] = new BsonBinaryData(new byte[]
            {
                1,
                2,
                3,
                4,
                5,
                6
            }),
            ["Double"] = new BsonDouble(42.23),
            ["Double"] = new BsonDouble(42.23),
            ["Boolean"] = new BsonBoolean(true),
            ["BsonArray"] = new BsonArray(new[]
            {
                BsonBoolean.False,
                BsonBoolean.True
            }),
            ["String"] = new BsonString("String"),
            ["Null"] = BsonNull.Value,
            ["Nested"] = new BsonDocument()
            {
                ["Int32"] = new BsonInt32(42), ["Int64"] = new BsonInt64(42),
            }
        };
    }
}
