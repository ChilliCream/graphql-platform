using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate.Types;

public class InputObjectTypeTests : TypeTestBase
{
    [Fact]
    public void InputObjectType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInputObjectType(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn<StringType>()
                .Field("bar")
                .Type<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void InputObjectType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInputObjectType(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn(typeof(StringType))
                .Field("bar")
                .Type<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericInputObjectType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInputObjectType<SimpleInput>(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericInputObjectType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddInputObjectType<SimpleInput>(d => d
                .Name(dep => dep.Name + "Foo")
                .DependsOn(typeof(StringType)))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void Initialize_IgnoreProperty_PropertyIsNotInSchemaType()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(
            d => d.Field(f => f.Id).Ignore());

        // assert
        fooType = CreateType(fooType);
        Assert.Collection(fooType.Fields,
            t => Assert.Equal("name", t.Name));
    }

    [Fact]
    public void Initialize_UnignoreProperty_PropertyIsInSchemaType()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(d =>
        {
            d.Field(f => f.Id).Ignore();
            d.Field(f => f.Id).Ignore(false);
        });

        // assert
        fooType = CreateType(fooType);
        Assert.Collection(fooType.Fields,
            t => Assert.Equal("id", t.Name),
            t => Assert.Equal("name", t.Name));
    }

    [Fact]
    public void EnsureInputObjectTypeKindIsCorrect()
    {
        // arrange
        var schema = Create();
        var inputObjectType =
            schema.GetType<InputObjectType>("Object1");

        // act
        var kind = inputObjectType.Kind;

        // assert
        Assert.Equal(TypeKind.InputObject, kind);
    }

    [Fact]
    public void GenericInputObject_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(
            d => d.Directive("foo").Field(f => f.Id).Directive("foo"));

        // assert
        fooType = CreateType(fooType, b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void GenericInputObject_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(
            d => d.Directive("foo").Field(f => f.Id).Directive("foo"));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void GenericInputObject_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(d => d
            .Name("Bar")
            .Directive(new DirectiveNode("foo"))
            .Field(f => f.Id)
            .Directive(new DirectiveNode("foo")));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void GenericInputObject_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(d => d
            .Name("Bar")
            .Directive(new FooDirective())
            .Field(f => f.Id)
            .Directive(new FooDirective()));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void GenericInputObject_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(d => d
            .Name("Bar")
            .Directive<FooDirective>()
            .Field(f => f.Id)
            .Directive<FooDirective>());

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InputObject_AddDirectives_NameArgs()
    {
        // arrange
        // act
        var fooType = new InputObjectType(d => d
            .Name("Bar")
            .Directive("foo")
            .Field("id")
            .Type<StringType>()
            .Directive("foo"));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InputObject_AddDirectives_NameArgs2()
    {
        // arrange
        // act
        var fooType = new InputObjectType<SimpleInput>(d => d
            .Name("Bar")
            .Directive("foo")
            .Field("id")
            .Type<StringType>()
            .Directive("foo"));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InputObject_AddDirectives_DirectiveNode()
    {
        // arrange
        // act
        var fooType = new InputObjectType(d => d
            .Name("Bar")
            .Directive(new DirectiveNode("foo"))
            .Field("id")
            .Type<StringType>()
            .Directive(new DirectiveNode("foo")));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InputObject_AddDirectives_DirectiveClassInstance()
    {
        // arrange
        // act
        var fooType = new InputObjectType(d => d
            .Name("Bar")
            .Directive(new FooDirective())
            .Field("id")
            .Type<StringType>()
            .Directive(new FooDirective()));

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    [Fact]
    public void InputObject_AddDirectives_DirectiveType()
    {
        // arrange
        // act
        var fooType = new InputObjectType(d => d
            .Name("Bar")
            .Directive<FooDirective>()
            .Field("id")
            .Type<StringType>()
            .Directive<FooDirective>());

        // assert
        fooType = CreateType(fooType,
            b => b.AddDirectiveType<FooDirectiveType>());

        Assert.NotEmpty(fooType.Directives["foo"]);
        Assert.NotEmpty(fooType.Fields["id"].Directives["foo"]);
    }

    private ISchema Create()
    {
        return SchemaBuilder.New()
            .ModifyOptions(o => o.StrictValidation = false)
            .AddInputObjectType<SerializationInputObject1>(d =>
            {
                d.Name("Object1");
                d.Field(t => t.Foo).Type<InputObjectType<SerializationInputObject2>>();
                d.Field(t => t.Bar).Type<StringType>();
            })
            .AddInputObjectType<SerializationInputObject2>(d =>
            {
                d.Name("Object2");
                d.Field(t => t.FooList)
                    .Type<NonNullType<ListType<InputObjectType<SerializationInputObject1>>>>();
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();
    }

    [Fact]
    public void DoNotAllow_InputTypes_OnFields()
    {
        // arrange
        // act
        void Action() => SchemaBuilder.New()
            .AddType(new InputObjectType(t => t.Name("Foo")
                .Field("bar")
                .Type<NonNullType<ObjectType<SimpleInput>>>()))
            .Create();

        // assert
        Assert.Throws<SchemaException>(Action).Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void DoNotAllow_DynamicInputTypes_OnFields()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddType(new InputObjectType(t => t.Name("Foo")
                    .Field("bar")
                    .Type(new NonNullType(new ObjectType<SimpleInput>()))))
                .Create();

        // assert
        var ex = Assert.Throws<SchemaException>(Action).Errors.First().Exception;
        Assert.Equal("inputType", Assert.IsType<ArgumentException>(ex).ParamName);
    }

    [Fact]
    public void Ignore_DescriptorIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action()
            => InputObjectTypeDescriptorExtensions.Ignore<SimpleInput>(null, t => t.Id);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Ignore_ExpressionIsNull_ArgumentNullException()
    {
        // arrange
        var descriptor =
            InputObjectTypeDescriptor.New<SimpleInput>(
                DescriptorContext.Create());

        // act
        void Action() => descriptor.Ignore(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Ignore_Id_Property()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(new InputObjectType<SimpleInput>(d => d
                .Ignore(t => t.Id)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Unignore_Id_Property()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(new InputObjectType<SimpleInput>(d =>
            {
                d.Ignore(t => t.Id);
                d.Field(t => t.Id).Ignore(false);
            }))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Convert_Parts_Of_The_Input_Graph()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryType>()
            .AddTypeConverter<Baz, Bar>(from => new Bar { Text = from.Text, })
            .AddTypeConverter<Bar, Baz>(from => new Baz { Text = from.Text, })
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ foo(a: { bar: { text: \"abc\" } }) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void Ignore_Fields_With_GraphQLIgnoreAttribute()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType<InputObjectType<FooIgnored>>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Input_With_Optionals_Not_Set()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithOptionals>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ do(input: { baz: \"abc\" }) { isBarSet bar baz } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [InlineData("null")]
    [InlineData("\"abc\"")]
    [Theory]
    public async Task Input_With_Optionals_Set(string value)
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithOptionals>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ do(input: { bar: " + value + " }) { isBarSet } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Input_With_Immutable_ClrType()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithImmutables>()
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "{ do(input: { bar: \"abc\" baz: \"def\" qux: \"ghi\" }) { bar baz qux } }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void Input_Infer_Default_Values()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(d => d
                .Name("Query")
                .Field("abc")
                .Argument("def", a => a.Type<InputObjectType<InputWithDefault>>())
                .Resolve("ghi"))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Input_IgnoreField()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithInterfaceInput>()
            .AddType<InputWithInterfaceType>()
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Specify_Argument_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddInputObjectType(d =>
            {
                d.Name("Bar");
                d.Field("Foo").Type("String");
            })
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    public class InputWithInterfaceType : InputObjectType<InputWithInterface>
    {
        protected override void Configure(
            IInputObjectTypeDescriptor<InputWithInterface> descriptor)
        {
            descriptor.Field(x => x.Works);
            descriptor.Field(x => x.DoesNotWork).Ignore();
        }
    }

    [Fact]
    public void InputObjectType_InInputObjectType_ThrowsSchemaException()
    {
        // arrange
        // act
        void Fail()
            => SchemaBuilder
                .New()
                .AddQueryType(x => x.Name("Query").Field("Foo").Resolve("bar"))
                .AddType<InputObjectType<InputObjectType<Foo>>>()
                .ModifyOptions(o => o.StrictRuntimeTypeValidation = true)
                .Create();

        // assert
        Assert.Throws<SchemaException>(Fail).Message.MatchSnapshot();
    }

    [Fact]
    public async Task Input_Casing_Is_Recognized()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddInputObjectType<FieldNameInput>()
            .ModifyOptions(o => o.StrictValidation = false)
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DepreactedInputTypes_NullableFields_Valid()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddInputObjectType<DeprecatedInputFields>()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DepreactedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .AddInputObjectType<DeprecatedNonNull>()
            .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DepreactedInputTypes_NullableFields_Valid()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .AddInputObjectType(x => x.Name("Foo").Field("bar").Type<IntType>().Deprecated("b"))
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DepreactedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .AddInputObjectType(x => x
                .Name("Foo")
                .Field("bar")
                .Type<NonNullType<IntType>>()
                .Deprecated("b"))
            .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DepreactedInputTypes_NullableFields_Valid()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .AddDocumentFromString(@"
                    input Foo {
                        bar: String @deprecated(reason: ""reason"")
                    }
                ")
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DepreactedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd"))
            .AddDocumentFromString(@"
                    input Foo {
                        bar: String! @deprecated(reason: ""reason"")
                    }
                ")
            .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public void OneOf_descriptor_is_null()
    {
        void Fail() => InputObjectTypeDescriptorExtensions.OneOf(null);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void OneOf_generic_descriptor_is_null()
    {
        void Fail() => InputObjectTypeDescriptorExtensions.OneOf<object>(null);

        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Deprecate_fields_with_attribute()
    {
        SchemaBuilder.New()
            .AddInputObjectType<InputWithDeprecatedField>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Ignore_Methods()
    {
        SchemaBuilder.New()
            .AddInputObjectType<FooWithMethod>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .Print()
            .MatchSnapshot();
    }

    public class FieldNameInput
    {
        public string? YourFieldName { get; set; }

        public string YourFieldname { get; set; } = default!;
    }

    public class DeprecatedInputFields
    {
        [Obsolete("reason")]
        public int? ObsoleteWithReason { get; set; }

        [Obsolete]
        public int? Obsolete { get; set; }

        [GraphQLDeprecated("reason")]
        public int? Deprecated { get; set; }
    }

    public class DeprecatedNonNull
    {
        [Obsolete("reason")]
        public int ObsoleteWithReason { get; set; }
    }

    public class QueryWithInterfaceInput
    {
        public string? Test(InputWithInterface? input) => "Foo";
    }

    public class InputWithInterface
    {
        public string? Works { get; set; }
        public IDoesNotWork? DoesNotWork { get; set; }
    }

    public interface IDoesNotWork
    {
        public double? Member { get; set; }
    }

    public class SimpleInput
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class SerializationInputObject1
    {
        public SerializationInputObject2? Foo { get; set; }
        public string? Bar { get; set; } = "Bar";
    }

    public class SerializationInputObject2
    {
        public List<SerializationInputObject1?>? FooList { get; set; } =
            [new SerializationInputObject1(),];
    }

    public class FooDirectiveType : DirectiveType<FooDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<FooDirective> descriptor)
        {
            descriptor
                .Name("foo")
                .Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.InputFieldDefinition);
        }
    }

    public class FooDirective;

    public class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Name("Query")
                .Field("foo")
                .Argument("a", a => a.Type<FooInputType>())
                .Type<StringType>()
                .Resolve(ctx => ctx.ArgumentValue<Foo>("a").Bar?.Text);
        }
    }

    public class FooInputType : InputObjectType<Foo>
    {
        protected override void Configure(
            IInputObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar).Type<BazInputType>();
        }
    }

    public class BazInputType : InputObjectType<Baz>
    {
    }

    public class Foo
    {
        public Bar? Bar { get; set; }
    }

    public class FooIgnored
    {
        [GraphQLIgnore]
        public Bar? Bar { get; set; }

        public Bar? Baz { get; set; }
    }

    public class Bar
    {
        public string? Text { get; set; }
    }

    public class Baz
    {
        public string? Text { get; set; }
    }

    public class QueryWithOptionals
    {
        public FooPayload Do(FooInput input)
        {
            return new FooPayload
            {
                IsBarSet = input.Bar.HasValue,
                Bar = input.Bar,
                Baz = input.Baz,
            };
        }
    }

    public class FooInput
    {
        public Optional<string?> Bar { get; set; }
        public string? Baz { get; set; }
    }

    public class FooPayload
    {
        public bool IsBarSet { get; set; }
        public string? Bar { get; set; }
        public string? Baz { get; set; }
    }

    public class FooWithMethod
    {
        public bool IsBarSet { get; set; }

        public string? Bar() => null;
    }

    public class QueryWithImmutables
    {
        public FooImmutable? Do(FooImmutable? input)
        {
            return input;
        }
    }

    public class FooImmutable
    {
        public FooImmutable()
        {
            Bar = "default";
        }

        public FooImmutable(string? bar, string? baz)
        {
            Bar = bar;
            Baz = baz;
        }

        public string? Bar { get; }

        public string? Baz { get; set; }

        public string? Qux { get; private set; }
    }

    public class InputWithDefault
    {
        [DefaultValue("abc")]
        public string? WithStringDefault { get; set; }

        [DefaultValue(null)]
        public string? WithNullDefault { get; set; }

        [DefaultValue(FooEnum.Bar)]
        public FooEnum Enum { get; set; }

        [DefaultValueSyntax("[[{ foo: 1 } ]]")]
        public List<List<ComplexInput>> ComplexInput { get; set; } = null!;

        public string? WithoutDefault { get; set; }
    }

    public class ComplexInput
    {
        public int Foo { get; set; }
    }

    public class InputWithDeprecatedField
    {
        [Obsolete]
        public string? A { get; set; }

        [GraphQLDeprecated("Foo Bar")]
        public string? B { get; set; }
    }

    public enum FooEnum
    {
        Bar,
        Baz,
    }
}
