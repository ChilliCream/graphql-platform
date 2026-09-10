using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Tests;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

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

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field1": "abc",
                "field2": 123
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseInputValue(fieldData.RootElement, type, context.Object, Path.Root);

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

        var type = schema.Types.GetType<InputObjectType>("TestInput");

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

        var type = schema.Types.GetType<InputObjectType>("Test2Input");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field1": "abc",
                "field2": 123
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseInputValue(fieldData.RootElement, type, context.Object, Path.Root);

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

        var type = schema.Types.GetType<InputObjectType>("Test2Input");

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

        var type = schema.Types.GetType<InputObjectType>("Test2Input");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field2": 123
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        void Action() => parser.ParseInputValue(fieldData.RootElement, type, context.Object, Path.Root);

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_MissingRequired()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test2Input>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("Test2Input");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        void Action() => parser.ParseLiteral(fieldData, type, Path.Root);

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_OneInvalidField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field2": 123,
                "field3": 123
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseInputValue(
                fieldData.RootElement,
                type,
                context: context.Object,
                Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_OneInvalidField()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Deserialize_InputObject_AllIsSet_TwoInvalidFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field2": 123,
                "field3": 123,
                "field4": 123
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseInputValue(
                fieldData.RootElement,
                type,
                context: context.Object,
                path: Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_TwoInvalidFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123),
            new ObjectFieldNode("field4", 123));

        // act
        var parser = new InputParser(new DefaultTypeConverter());

        void Action()
            => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_AllIsSet_IgnoreAdditionalInputFields()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<TestInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("TestInput");

        var fieldData = new ObjectValueNode(
            new ObjectFieldNode("field1", "abc"),
            new ObjectFieldNode("field2", 123),
            new ObjectFieldNode("field3", 123),
            new ObjectFieldNode("field4", 123));

        var converter = new DefaultTypeConverter();

        var options = new InputParserOptions
        {
            IgnoreAdditionalInputFields = true
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

        var type = schema.Types.GetType<InputObjectType>("Test3Input");

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

        var type = new NonNullType(schema.Types.GetType<InputObjectType>("Test3Input"));

        // act
        var parser = new InputParser();

        void Action()
            => parser.ParseLiteral(
                NullValueNode.Default,
                type,
                Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Action).MatchSnapshot();
    }

    [Fact]
    public void Parse_InputObject_NullableEnumList()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<FooInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new NonNullType(schema.Types.GetType<InputObjectType>("FooInput"));

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
    public void Deserialize_List_Can_Be_Coerced_From_Single_Value()
    {
        // arrange
        var parser = new InputParser(new DefaultTypeConverter());
        var type = (IType)new ListType(new BooleanType());
        var inputValue = JsonDocument.Parse("true");

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var runtimeValue =
            parser.ParseInputValue(inputValue.RootElement, type, context.Object, Path.Root.Append("root"));

        // assert
        Assert.Collection(Assert.IsType<List<bool?>>(runtimeValue), Assert.True);
    }

    [Fact]
    public void Parse_ListOfInputObjects_WithoutRuntimeBinding_ReturnsRuntimeList()
    {
        // arrange
        // LinkInput is bound to the runtime type Link but its fields are not bound
        // to CLR properties, so each element is materialized as a dictionary.
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Link>(d =>
            {
                d.Name("LinkInput");
                d.BindFieldsExplicitly();
                d.Field("url").Type<StringType>();
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new ListType(schema.Types.GetType<InputObjectType>("LinkInput"));

        var listData = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("url", "https://a")),
            new ObjectValueNode(new ObjectFieldNode("url", "https://b")));

        // act
        var parser = new InputParser();
        var runtimeValue = parser.ParseLiteral(listData, type, Path.Root.Append("root"));

        // assert
        Assert.Collection(
            Assert.IsType<List<Link>>(runtimeValue),
            t => Assert.Equal("https://a", t.Url),
            t => Assert.Equal("https://b", t.Url));
    }

    [Fact]
    public void Deserialize_ListOfInputObjects_WithoutRuntimeBinding_ReturnsRuntimeList()
    {
        // arrange
        // LinkInput is bound to the runtime type Link but its fields are not bound
        // to CLR properties, so each element is materialized as a dictionary.
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Link>(d =>
            {
                d.Name("LinkInput");
                d.BindFieldsExplicitly();
                d.Field("url").Type<StringType>();
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new ListType(schema.Types.GetType<InputObjectType>("LinkInput"));

        var inputValue = JsonDocument.Parse(
            """
            [{ "url": "https://a" }, { "url": "https://b" }]
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseInputValue(
            inputValue.RootElement, type, context.Object, Path.Root.Append("root"));

        // assert
        Assert.Collection(
            Assert.IsType<List<Link>>(runtimeValue),
            t => Assert.Equal("https://a", t.Url),
            t => Assert.Equal("https://b", t.Url));
    }

    [Fact]
    public void Parse_SingleInputObject_CoercedIntoList_WithoutRuntimeBinding()
    {
        // arrange
        // A single object value (not wrapped in a list) is coerced into a one-element list.
        var schema = SchemaBuilder.New()
            .AddType<LinkInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new ListType(schema.Types.GetType<InputObjectType>("LinkInput"));

        var singleData = new ObjectValueNode(new ObjectFieldNode("url", "https://a"));

        // act
        var parser = new InputParser();
        var runtimeValue = parser.ParseLiteral(singleData, type, Path.Root.Append("root"));

        // assert
        Assert.Collection(
            Assert.IsType<List<Link>>(runtimeValue),
            t => Assert.Equal("https://a", t.Url));
    }

    [Fact]
    public void Deserialize_SingleInputObject_CoercedIntoList_WithoutRuntimeBinding()
    {
        // arrange
        // A single JSON object is coerced into a one-element list.
        var schema = SchemaBuilder.New()
            .AddType<LinkInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new ListType(schema.Types.GetType<InputObjectType>("LinkInput"));

        var inputValue = JsonDocument.Parse(
            """
            { "url": "https://a" }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseInputValue(
            inputValue.RootElement, type, context.Object, Path.Root.Append("root"));

        // assert
        Assert.Collection(
            Assert.IsType<List<Link>>(runtimeValue),
            t => Assert.Equal("https://a", t.Url));
    }

    [Fact]
    public void Parse_ListOfInputObjects_WithoutField_SurfacesConversionError()
    {
        // arrange
        // No field context (the IType overload) and the converter fails the
        // dictionary -> Link conversion. The real conversion error must surface
        // instead of a misleading IList.Add type-mismatch error.
        var schema = SchemaBuilder.New()
            .AddType<LinkInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = new ListType(schema.Types.GetType<InputObjectType>("LinkInput"));

        var listData = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("url", "https://a")));

        var parser = new InputParser(new ThrowingTypeConverter());

        // act
        void Act() => parser.ParseLiteral(listData, type, Path.Root.Append("root"));

        // assert
        Assert.Equal("conversion boom", Assert.Throws<InvalidOperationException>(Act).Message);
    }

    [Fact]
    public void Parse_ListOfInputObjects_WithField_ReportsCoercionError_OnConversionFailure()
    {
        // arrange
        // With a field context the failed conversion is reported as a coercion error.
        var schema = SchemaBuilder.New()
            .AddType<LinkContainerInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var container = schema.Types.GetType<InputObjectType>("LinkContainerInput");
        var linksField = container.Fields["links"];

        var listData = new ListValueNode(
            new ObjectValueNode(new ObjectFieldNode("url", "https://a")));

        var parser = new InputParser(new ThrowingTypeConverter());

        // act
        void Act() => parser.ParseLiteral(listData, linksField);

        // assert
        Assert.IsType<LeafCoercionException>(Assert.ThrowsAny<Exception>(Act));
    }

    [Fact]
    public async Task Integration_InputObjectDefaultValue_ValueIsInitialized()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query4>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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

        var oneOfInput = schema.Types.GetType<InputObjectType>(nameof(OneOfInput));

        var parser = new InputParser();

        var data = new ObjectValueNode(
            new ObjectFieldNode("a", "abc"),
            new ObjectFieldNode("b", 123));

        // act
        void Fail() => parser.ParseLiteral(data, oneOfInput, Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Fail).Errors.MatchSnapshot();
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

        var oneOfInput = schema.Types.GetType<InputObjectType>(nameof(OneOfInput));

        var parser = new InputParser();

        var data = new ObjectValueNode(
            new ObjectFieldNode("a", NullValueNode.Default),
            new ObjectFieldNode("b", 123));

        // act
        void Fail()
            => parser.ParseLiteral(data, oneOfInput, Path.Root.Append("root"));

        // assert
        Assert.Throws<LeafCoercionException>(Fail).Errors.MatchSnapshot();
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

        var oneOfInput = schema.Types.GetType<InputObjectType>(nameof(OneOfInput));

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

        var type = schema.Types.GetType<InputObjectType>("Test4Input");

        var fieldData = JsonDocument.Parse(
            """
            {
                "field1": "abc"
            }
            """);

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        // act
        var parser = new InputParser(new DefaultTypeConverter());
        var runtimeValue = parser.ParseInputValue(fieldData.RootElement, type, context.Object, Path.Root);

        // assert
        Assert.IsType<Test4Input>(runtimeValue).MatchSnapshot();
    }

    [Fact]
    public void Force_NonNull_Struct_Constructor_Parameter_To_Be_Optional()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddInputObjectType<Test5Input>(d =>
            {
                d.Field(t => t.Field2).Type<IntType>();
                d.Field(t => t.Field3).Type<BooleanType>();
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        var type = schema.Types.GetType<InputObjectType>("Test5Input");

        var context = new Mock<IFeatureProvider>();
        context.Setup(t => t.Features).Returns(FeatureCollection.Empty);

        var parser = new InputParser(new DefaultTypeConverter());

        using var missingFieldsData = JsonDocument.Parse(
            """
            {
                "field1": "abc"
            }
            """);

        using var explicitNullData = JsonDocument.Parse(
            """
            {
                "field1": "abc",
                "field2": null,
                "field3": null
            }
            """);

        // act
        var missingFields = Assert.IsType<Test5Input>(
            parser.ParseInputValue(
                missingFieldsData.RootElement,
                type,
                context.Object,
                Path.Root));
        var explicitNull = Assert.IsType<Test5Input>(
            parser.ParseInputValue(
                explicitNullData.RootElement,
                type,
                context.Object,
                Path.Root));

        // assert
        Assert.Equal("abc", missingFields.Field1);
        Assert.Equal(0, missingFields.Field2);
        Assert.False(missingFields.Field3);
        Assert.Equal("abc", explicitNull.Field1);
        Assert.Equal(0, explicitNull.Field2);
        Assert.False(explicitNull.Field3);
    }

    [Fact]
    public async Task Integration_CodeFirst_InputObjectNoDefaultValue_NoRuntimeTypeDefaultValueIsInitialized()
    {
        // arrange
        var resolverArgumentsAccessor = new ResolverArgumentsAccessor();
        var executor = await new ServiceCollection()
            .AddSingleton(resolverArgumentsAccessor)
            .AddGraphQL()
            .AddQueryType(x => x.Field("foo")
                .Argument("args", a => a.Type<NonNullType<MyInputType>>())
                .Type<StringType>()
                .ResolveWith<ResolverArgumentsAccessor>(r => r.ResolveWith(default!)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var query =
            OperationRequest.FromSourceText(
                """
                {
                    a: foo(args: { string: "allSet" int: 1 bool: true })
                    b: foo(args: { string: "noneSet" })
                    c: foo(args: { string: "intExplicitlyNull" int: null })
                    d: foo(args: { string: "boolExplicitlyNull" bool: null })
                    e: foo(args: { string: "intSetBoolNull" int: 1 bool: null })
                    f: foo(args: { string: "boolSetIntNull" int: null bool: true })
                }
                """);
        await executor.ExecuteAsync(query, CancellationToken.None);

        // assert
        resolverArgumentsAccessor.Arguments.MatchSnapshot();
    }

    private class ResolverArgumentsAccessor
    {
#if NET9_0_OR_GREATER
        private readonly Lock _lock = new();
#else
        private readonly object _lock = new();
#endif
        internal SortedDictionary<string, IDictionary<string, object?>?> Arguments { get; } = new();

        internal string? ResolveWith(IDictionary<string, object?> args)
        {
            lock (_lock)
            {
                Arguments[args["string"]!.ToString()!] = args;
            }

            return "OK";
        }
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
        Baz
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
        public string Field1 { get; set; } = null!;

        public int Field2 { get; set; }
    }

    public class Test5Input
    {
        public Test5Input(string field1, int field2, bool field3)
        {
            Field1 = field1;
            Field2 = field2;
            Field3 = field3;
        }

        public string Field1 { get; }

        public int Field2 { get; }

        public bool Field3 { get; }
    }

    public class MyInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("MyInput");
            descriptor.Field("string").Type<StringType>();
            descriptor.Field("int").Type<IntType>();
            descriptor.Field("bool").Type<BooleanType>();
        }
    }

    public class Link
    {
        public string? Url { get; set; }
    }

    public class LinkInput : InputObjectType<Link>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Link> descriptor)
        {
            descriptor.Name("LinkInput");
            descriptor.BindFieldsExplicitly();
            descriptor.Field("url").Type<StringType>();
        }
    }

    public class LinkContainer
    {
        public List<Link>? Links { get; set; }
    }

    public class LinkContainerInput : InputObjectType<LinkContainer>
    {
        protected override void Configure(IInputObjectTypeDescriptor<LinkContainer> descriptor)
        {
            descriptor.Name("LinkContainerInput");
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Links).Type<ListType<LinkInput>>();
        }
    }

    private sealed class ThrowingTypeConverter : ITypeConverter
    {
        public bool TryConvert(
            Type from,
            Type to,
            object? source,
            out object? converted,
            out Exception? conversionException)
        {
            converted = null;
            conversionException = to == typeof(Link)
                ? new InvalidOperationException("conversion boom")
                : null;
            return false;
        }

        public object? Convert(Type from, Type to, object? source)
            => throw new NotSupportedException();
    }
}
