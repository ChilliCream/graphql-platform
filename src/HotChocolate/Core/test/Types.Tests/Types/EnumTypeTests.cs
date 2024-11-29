using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HotChocolate.Types;

public class EnumTypeTests : TypeTestBase
{
    [Fact]
    public void EnumType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType(d => d
                .Name(dep => dep.Name + "Enum")
                .DependsOn<StringType>()
                .Value("BAR"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("StringEnum");
        Assert.NotNull(type);
    }

    [Fact]
    public void EnumType_GraphQLDescriptionAttribute()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType<DescriptionTestEnum>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("DescriptionTestEnum");
        Assert.Equal("TestDescription", type.Description);
    }

    [Fact]
    public void EnumType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType(d => d
                .Name(dep => dep.Name + "Enum")
                .DependsOn(typeof(StringType))
                .Value("BAR"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("StringEnum");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericEnumType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType(d => d
                .Name(dep => dep.Name + "Enum")
                .DependsOn<StringType>()
                .Value("ABC"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("StringEnum");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericEnumType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType(d => d
                .Name(dep => dep.Name + "Enum")
                .DependsOn(typeof(StringType))
                .Value("ABC"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("StringEnum");
        Assert.NotNull(type);
    }

    [Fact]
    public void EnumType_WithDirectives()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddDirectiveType(new DirectiveType(d => d
                .Name("bar")
                .Location(DirectiveLocation.Enum)))
            .AddEnumType(d => d.Name("Foo").Directive(new DirectiveNode("bar")).Value("ABC"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Directives, t => Assert.Equal("bar", t.Type.Name));
    }

    [Fact]
    public void EnumType_WithDirectivesT()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddDirectiveType(new DirectiveType<Bar>(d => d
                .Name("bar")
                .Location(DirectiveLocation.Enum)))
            .AddEnumType(d => d.Name("Foo").Directive<Bar>().Value("ABC"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Directives,
            t => Assert.Equal("bar", t.Type.Name));
    }

    [Fact]
    public void ImplicitEnumType_DetectEnumValues()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType<Foo>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.NotNull(type);
        Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
        Assert.Equal(Foo.Bar1, value);
        Assert.True(type.TryGetRuntimeValue("BAR2", out value));
        Assert.Equal(Foo.Bar2, value);
    }

    [Fact]
    public void ExplicitEnumType_OnlyContainDeclaredValues()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType<Foo>(d =>
            {
                d.BindValues(BindingBehavior.Explicit);
                d.Value(Foo.Bar1);
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.NotNull(type);
        Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
        Assert.Equal(Foo.Bar1, value);
        Assert.False(type.TryGetRuntimeValue("BAR2", out value));
        Assert.Null(value);
    }

    [Fact]
    public void ExplicitEnumType_OnlyContainDeclaredValues_2()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType<Foo>(d =>
            {
                d.BindValuesImplicitly().BindValuesExplicitly();
                d.Value(Foo.Bar1);
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.NotNull(type);
        Assert.True(type.TryGetRuntimeValue("BAR1", out var value));
        Assert.Equal(Foo.Bar1, value);
        Assert.False(type.TryGetRuntimeValue("BAR2", out value));
        Assert.Null(value);
    }

    [Fact]
    public void EnumTypeT_Ignore_Fields()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<FooIgnoredType>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void EnumTypeT_Ignore_Fields_With_Extension()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<FooIgnoredTypeWithExtension>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ImplicitEnumType_OnlyBar1HasCustomName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddEnumType<Foo>(d =>
            {
                d.Value(Foo.Bar1).Name("FOOBAR");
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.NotNull(type);

        Assert.Collection(
            type.Values,
            t =>
            {
                Assert.Equal(Foo.Bar1, t.Value);
                Assert.Equal("FOOBAR", t.Name);
            },
            t =>
            {
                Assert.Equal(Foo.Bar2, t.Value);
                Assert.Equal("BAR2", t.Name);
            });
    }

    [Fact]
    public void EnumType_WithNoValues()
    {
        // act
        void Action() => SchemaBuilder.New().AddType<EnumType>().Create();

        // assert
        Assert.Throws<SchemaException>(Action);
    }

    [Fact]
    public void EnsureEnumTypeKindIsCorrect()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddType<EnumType<Foo>>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Equal(TypeKind.Enum, type.Kind);
    }

    [Fact]
    public void EnumValue_ValueIsNull_SchemaException()
    {
        // arrange
        // act
        void Action() => SchemaBuilder.New()
            .AddQueryType<Bar>()
            .AddType(new EnumType(d => d.Name("Foo")
                .Value<string>(null)))
            .Create();

        // assert
        Assert.Throws<SchemaException>(Action)
            .Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public void EnumValueT_ValueIsNull_SchemaException()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType<Bar>()
                .AddType(new EnumType<Foo?>(d => d.Name("Foo")
                    .Value(null)))
                .Create();

        // assert

        var ex =
            Assert.Throws<SchemaException>(Action)
                .Errors.Single().Exception;

        Assert.Equal(
            "runtimeValue",
            Assert.IsType<ArgumentNullException>(ex).ParamName);
    }

    [Fact]
    public void EnumValue_WithDirectives()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .Value("baz")
                .Directive(new DirectiveNode("bar")))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Values,
            v => Assert.Collection(v.Directives,
                t => Assert.Equal("bar", t.Type.Name)));
    }

    [Fact]
    public void EnumValue_WithDirectivesNameArgs()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .Value("baz")
                .Directive("bar", []))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Values,
            v => Assert.Collection(v.Directives,
                t => Assert.Equal("bar", t.Type.Name)));
    }

    [Fact]
    public void Serialize_EnumValue_WithDirectives()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .Value("baz")
                .Directive(new DirectiveNode("bar")))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void EnumValue_WithDirectivesT()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType<Bar>(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .Value("baz")
                .Directive<Bar>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Values,
            v => Assert.Collection(v.Directives,
                t => Assert.Equal("bar", t.Type.Name)));
    }

    [Fact]
    public void EnumValue_WithDirectivesTInstance()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType<Bar>(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .Value("baz")
                .Directive(new Bar()))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Values,
            v => Assert.Collection(v.Directives,
                t => Assert.Equal("bar", t.Type.Name)));
    }

    [Fact]
    public void EnumValue_SetContextData()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddEnumType(d => d
                .Name("Foo")
                .Value("bar")
                .Extend()
                .OnBeforeCreate(def => def.ContextData["baz"] = "qux"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.Collection(type.Values,
            v => Assert.Collection(v.ContextData,
                c =>
                {
                    Assert.Equal("baz", c.Key);
                    Assert.Equal("qux", c.Value);
                }));
    }

    [Fact]
    public void EnumValue_DefinitionIsNull_ArgumentNullException()
    {
        // arrange
        var completionContext = new Mock<ITypeCompletionContext>();

        // act
        void Action() => new EnumValue(completionContext.Object, null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumValue_ContextIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => new EnumValue(null!, new EnumValueDefinition());

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void EnumValue_DefinitionValueIsNull_ArgumentNullException()
    {
        // arrange
        var completionContext = new Mock<ITypeCompletionContext>();

        // act
        void Action() => new EnumValue(completionContext.Object, new EnumValueDefinition());

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    [Fact]
    public void Deprecate_Obsolete_Values()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<FooObsolete>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Deprecate_Fields_With_Deprecated_Attribute()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<FooDeprecated>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Ignore_Fields_With_GraphQLIgnoreAttribute()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<FooIgnore>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void EnumType_That_Is_Bound_To_String_Should_Not_Interfere_With_Scalar()
    {
        SchemaBuilder.New()
            .AddQueryType<SomeQueryType>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Recognize_GraphQLNameAttribute_On_EnumType_And_EnumValue()
    {
        SchemaBuilder.New()
            .AddEnumType<FooName>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void ValueContainingUnderline_Should_NotResultInDoubleUnderline()
    {
        SchemaBuilder.New()
            .AddEnumType<FooUnderline>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Generic_Ignore_Descriptor_Is_Null()
    {
        void Fail()
            => EnumTypeDescriptorExtensions.Ignore<int>(default(IEnumTypeDescriptor<int>)!, 1);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Generic_Ignore_Value_Is_Null()
    {
        var descriptor = new Mock<IEnumTypeDescriptor<int?>>();

        void Fail()
            => EnumTypeDescriptorExtensions.Ignore<int?>(descriptor.Object, null);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Ignore_Descriptor_Is_Null()
    {
        void Fail()
            => EnumTypeDescriptorExtensions.Ignore<int>(default(IEnumTypeDescriptor)!, 1);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Ignore_Value_Is_Null()
    {
        var descriptor = new Mock<IEnumTypeDescriptor>();

        void Fail()
            => EnumTypeDescriptorExtensions.Ignore<int?>(descriptor.Object, null);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void EnumName_Set_Name_Comparer()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType<Bar>(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .NameComparer(StringComparer.OrdinalIgnoreCase)
                .Value("baz")
                .Name("BAZ"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.True(type.IsInstanceOfType(new EnumValueNode("baz")));
    }

    [Fact]
    public void EnumName_Set_Value_Comparer()
    {
        // act
        var schema = SchemaBuilder
            .New()
            .AddDirectiveType(new DirectiveType<Bar>(d => d
                .Name("bar")
                .Location(DirectiveLocation.EnumValue)))
            .AddEnumType(d => d
                .Name("Foo")
                .ValueComparer(new ValueComparer())
                .Value("baz")
                .Name("BAZ"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.True(type.IsInstanceOfType("ANYTHING WILL DO"));
    }

    [Fact]
    public async Task EnsureEnumValueOrder()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithEnum>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task EnsureEnumValueOrder_With_Introspection()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithEnum>()
                .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
                __type(name: "CriticalityLevel") {
                    enumValues {
                        name
                    }
                }
            }
            """);

        result.MatchMarkdownSnapshot();
    }

    public enum Foo
    {
        Bar1,
        Bar2,
    }

    public class Bar;

    public enum FooObsolete
    {
        Bar1,

        [Obsolete] Bar2,
    }

    public enum FooIgnore
    {
        Bar1,
        [GraphQLIgnore] Bar2,
    }

    public enum CriticalityLevel
    {
        Info,
        Warning,
        Critical
    }

    public class QueryWithEnum
    {
        public CriticalityLevel GetCriticalityLevel() => CriticalityLevel.Critical;
    }

    public class FooIgnoredType : EnumType<Foo>
    {
        protected override void Configure(IEnumTypeDescriptor<Foo> descriptor)
        {
            descriptor.Value(Foo.Bar2).Ignore();
        }
    }

    public class FooIgnoredTypeWithExtension : EnumType<Foo>
    {
        protected override void Configure(IEnumTypeDescriptor<Foo> descriptor)
        {
            descriptor.Ignore(Foo.Bar2);
        }
    }

    public enum FooDeprecated
    {
        Bar1,
        [GraphQLDeprecated("Baz.")] Bar2,
    }

    [GraphQLName("Foo")]
    public enum FooName
    {
        Bar1,
        [GraphQLName("BAR_2")] Bar2,
    }

    public enum FooUnderline
    {
        Creating_Instance = 1,
    }

    public class SomeQueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("a").Type<SomeEnumType>().Resolve("DEF");
            descriptor.Field("b").Type<StringType>().Resolve("StringResolver");
        }
    }

    public class SomeEnumType
        : EnumType<string>
    {
        protected override void Configure(IEnumTypeDescriptor<string> descriptor)
        {
            descriptor.Name("Some");
            descriptor.Value("ABC").Name("DEF");
        }
    }

    [GraphQLDescription("TestDescription")]
    public enum DescriptionTestEnum
    {
        Foo,
        Bar,
    }

    public class ValueComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return true;
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return 1;
        }
    }
}
