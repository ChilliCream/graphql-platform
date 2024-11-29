using HotChocolate.Language;

namespace HotChocolate.Types;

public class EnumTypeExtensionTests
{
    [Fact]
    public void EnumTypeExtension_AddValue()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType<FooTypeExtension>()
            .Create();

        // assert
        var type = schema.GetType<FooType>("Foo");
        Assert.True(type.TryGetRuntimeValue("_QUOX", out _));
    }

    [Fact]
    public void EnumTypeExtension_AddValueThatDoesNotMatchClrType()
    {
        // arrange
        // act
        Action action = () => SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new EnumTypeExtension(d => d
                .Name("Foo")
                .Value("FOOBAR")))
            .Create();

        // assert
        Assert.Throws<SchemaException>(action);
    }

    [Fact]
    public void EnumTypeExtension_SetDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new EnumTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<FooType>("Foo");
        Assert.Collection(type.Directives["dummy"],
            t => { });
    }

    [Fact]
    public void EnumTypeExtension_ReplaceDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new EnumType<Foo>(d => d
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new EnumTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        var value = type.Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void EnumTypeExtension_RepeatableDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new EnumType<Foo>(d => d
                .Directive("dummy_rep")))
            .AddType(new EnumTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        var count = type.Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public void EnumTypeExtension_SetTypeContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new EnumTypeExtension(d => d
                .Name("Foo")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .Create();

        // assert
        var type = schema.GetType<EnumType>("Foo");
        Assert.True(type.ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void EnumTypeExtension_Ignore_Fields()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType<FooIgnoreTypeExtension>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    public class DummyQuery
    {
        public string Foo { get; set; }
    }

    public class FooType
        : EnumType<Foo>
    {
        protected override void Configure(
            IEnumTypeDescriptor<Foo> descriptor)
        {
            descriptor.BindValues(BindingBehavior.Explicit);
            descriptor.Value(Foo.Bar);
            descriptor.Value(Foo.Baz);
        }
    }

    public class FooTypeExtension
        : EnumTypeExtension
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Value(Foo.Quox).Name("_QUOX");
        }
    }

    public class FooIgnoreTypeExtension
        : EnumTypeExtension
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Value(Foo.Bar).Ignore();
        }
    }

    public enum Foo
    {
        Bar,
        Baz,
        Quox,
    }

    public class DummyDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy");
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class DummyWithArgDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_arg");
            descriptor.Argument("a").Type<StringType>();
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class RepeatableDummyDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_rep");
            descriptor.Repeatable();
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }
}
