using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;

#nullable enable

namespace HotChocolate.Types;

public class InputParserTests
{
    [Fact]
    public void Deserialize_InputObject_AllIsSet()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new Dictionary<string, object?>
        {
            { "field1", "abc" },
            { "field2", 123 },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseResult(fieldData, type, Path.Root);

        // assert
        Assert.IsType<TestInput>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field1", "abc"),
            new ObjectFieldNode("field2", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseLiteral(fieldData, type, Path.Root);

        // assert
        Assert.IsType<TestInput>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_ConstructorInit()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test2Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test2Input");

        var fieldData = new Dictionary<string, object?>
        {
            { "field1", "abc" },
            { "field2", 123 },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseResult(fieldData, type, Path.Root);

        // assert
        Assert.IsType<Test2Input>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_ConstructorInit()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test2Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test2Input");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field1", "abc"),
            new ObjectFieldNode("field2", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseLiteral(fieldData, type, Path.Root);

        // assert
        Assert.IsType<Test2Input>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_MissingRequired()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test2Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test2Input");

        var fieldData = new Dictionary<string, object?>
        {
            { "field2", 123 },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        void Action() => parser.ParseResult(fieldData, type, Path.Root);

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_MissingRequired()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test2Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test2Input");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        void Action() => parser.ParseLiteral(fieldData, type, Path.Root);

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_OneInvalidField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new Dictionary<string, object?>
        {
            { "field2", 123 },
            { "field3", 123 },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseResult(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_OneInvalidField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_TwoInvalidFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new Dictionary<string, object?>
        {
            { "field2", 123 },
            { "field3", 123 },
            { "field4", 123 },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseResult(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_TwoInvalidFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123),
            new ObjectFieldNode("field4", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_IgnoreAdditionalInputFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field1", "abc"),
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123),
            new ObjectFieldNode("field4", 123));

        var converter = new DefaultTypeConverter();

        var options = new InputParserOptions
        {
            IgnoreAdditionalInputFields = true,
        };

        // act
        var parser = new InputParser(converter, options);
        var runtimeValue = parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.IsType<TestInput>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_WithDefault_Values()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test3Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test3Input");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123));

        // act
        var parser = new InputParser();
        var obj = parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Equal("DefaultAbc", Assert.IsType<Test3Input>(obj).Field1);
    }

    [Fact]
    public void Parse_InputObject_NonNullViolation()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test3Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new NonNullType(schema.GetType<InputObjectType>("Test3Input"));

        // act
        var parser = new InputParser();

        void Action()
            => parser.ParseLiteral(
                NullValueNode.Default,
                type,
                Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_NullableEnumList()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<FooInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new NonNullType(schema.GetType<InputObjectType>("FooInput"));

        var listData = new ListValueNode(
            NullValueNode.Default,
            new EnumValueNode("BAZ"));

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("bars", listData));

        // act
        var parser = new InputParser();
        var runtimeData =
            parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Collection(
            Assert.IsType<FooInput>(runtimeData).Bars,
            t => Assert.Null(t),
            t => Assert.Equal(Bar.Baz, t));
    }

    [Fact]
    public async Task Integration_InputObjectDefaultValue_ValueIsInitialized()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query4>()
            .BuildRequestExecutorAsync();

        // act
        var query = 
            OperationRequest.FromSourceText(
                """
                {
                    loopback(input: {field2: 1}) {
                        field1
                        field2
                    }
                }
                """);
        var result = await executor.ExecuteAsync(query, CancellationToken.None);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void OneOf_A_and_B_Are_Set()
    {
        // arrange
        var schema =
            SchemaBuilder.New()
                .AddInputObjectType<OneOfInput>()
                .AddDirectiveType<OneOfDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

        var oneOfInput = schema.GetType<InputObjectType>(nameof(OneOfInput));

        var parser = new InputParser();

        var data = new ObjectValueNode(
            new ObjectFieldNode("a", "abc"),
            new ObjectFieldNode("b", 123));

        // act
        void Fail() => parser.ParseLiteral(data, oneOfInput, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Fail).Errors.MatchSnapshot();
    }

    [Fact]
    public void OneOf_A_is_Null_and_B_has_Value()
    {
        // arrange
        var schema =
            SchemaBuilder.New()
                .AddInputObjectType<OneOfInput>()
                .AddDirectiveType<OneOfDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

        var oneOfInput = schema.GetType<InputObjectType>(nameof(OneOfInput));

        var parser = new InputParser();

        var data = new ObjectValueNode(
            new ObjectFieldNode("a", NullValueNode.Default),
            new ObjectFieldNode("b", 123));

        // act
        void Fail()
            => parser.ParseLiteral(data, oneOfInput, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Fail).Errors.MatchSnapshot();
    }

    [Fact]
    public void OneOf_only_B_has_Value()
    {
        // arrange
        var schema =
            SchemaBuilder.New()
                .AddInputObjectType<OneOfInput>()
                .AddDirectiveType<OneOfDirectiveType>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

        var oneOfInput = schema.GetType<InputObjectType>(nameof(OneOfInput));

        var parser = new InputParser();

        var data = new ObjectValueNode(
            new ObjectFieldNode("b", 123));

        // act
        var runtimeValue =
            parser.ParseLiteral(data, oneOfInput, Path.Root.Append("root"));

        // assert
        runtimeValue.MatchSnapshot();
    }

    [Fact]
    public void Force_NonNull_Struct_To_Be_Optional()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test4Input>(d => d.Field(t => t.Field2).Type<IntType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.GetType<InputObjectType>("Test4Input");

        var fieldData = new Dictionary<string, object?>
        {
            { "field1", "abc" },
        };

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseResult(fieldData, type, Path.Root);

        // assert
        Assert.IsType<Test4Input>(runtimeValue).MatchSnapshot();
    }

    public class TestInput
    {
        public string? Field1 { get; set; }

        public int? Field2 { get; set; }
    }

    public class Test2Input
    {
        public Test2Input(string field1)
        {
            Field1 = field1;
        }

        public string Field1 { get; }

        public int? Field2 { get; set; }
    }

    public class Test3Input
    {
        public Test3Input(string field1)
        {
            Field1 = field1;
        }

        [DefaultValue("DefaultAbc")]
        public string Field1 { get; }

        public int? Field2 { get; set; }
    }

    public class FooInput
    {
        public List<Bar?> Bars { get; set; } = [];
    }

    public class Query4
    {
        public Test4 Loopback(Test4 input) => input;
    }

    public class Test4
    {
        [DefaultValue("DefaultAbc")]
        public string? Field1 { get; set; }

        public int? Field2 { get; set; }
    }

    public enum Bar
    {
        Baz,
    }

    [OneOf]
    public class OneOfInput
    {
        public string? A { get; set; }

        public int? B { get; set; }

        public string? C { get; set; }
    }

    public class Test4Input
    {
        public string Field1 { get; set; } = default!;

        public int Field2 { get; set; } = default!;
    }
}
