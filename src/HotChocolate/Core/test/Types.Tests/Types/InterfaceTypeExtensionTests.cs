#nullable enable

using HotChocolate.Language;

namespace HotChocolate.Types;

public class InterfaceTypeExtensionTests
{
    [Fact]
    public void InterfaceTypeExtension_AddField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType<FooTypeExtension>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields.ContainsField("test"));
    }

    [Fact]
    public void InterfaceTypeExtension_Deprecate_With_Reason()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Deprecated("Foo")))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["description"].IsDeprecated);
        Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
    }

    [Fact]
    public void InterfaceTypeExtension_Deprecate_Without_Reason()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Deprecated()))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["description"].IsDeprecated);
        Assert.Equal(
            WellKnownDirectives.DeprecationDefaultReason,
            type.Fields["description"].DeprecationReason);
    }

    [Fact]
    public void InterfaceTypeExtension_Deprecated_Directive_Is_Serialized()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Type<StringType>()
                .Deprecated()))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void InterfaceTypeExtension_SetTypeContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void InterfaceTypeExtension_SetFieldContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["description"]
            .ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void InterfaceTypeExtension_SetArgumentContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Type<StringType>()
                .Argument("a", a => a
                    .Type<StringType>()
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar"))))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["name"].Arguments["a"]
            .ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void InterfaceTypeExtension_SetDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void InterfaceTypeExtension_SetDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["name"].Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void InterfaceTypeExtension_SetDirectiveOnArgument()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a => a.Directive("dummy"))))
            .AddDirectiveType<DummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        Assert.True(type.Fields["name"].Arguments["a"].Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void InterfaceTypeExtension_ReplaceDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var value = type.Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InterfaceTypeExtension_ReplaceDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Field(f => f.Description)
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var value = type.Fields["description"].Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InterfaceTypeExtension_ReplaceDirectiveOnArgument()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Field(f => f.GetName(default!))
                .Argument("a", a => a
                    .Type<StringType>()
                    .Directive("dummy_arg", new ArgumentNode("a", "a")))))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a =>
                    a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
            .AddDirectiveType<DummyWithArgDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var value = type.Fields["name"].Arguments["a"]
            .Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InterfaceTypeExtension_CopyDependencies_ToType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType<FooType>()
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a =>
                    a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
            .AddDirectiveType<DummyWithArgDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var value = type.Fields["name"].Arguments["a"]
            .Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InterfaceTypeExtension_RepeatableDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Directive("dummy_rep")))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var count = type.Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public void InterfaceTypeExtension_RepeatableDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Field(f => f.Description)
                .Directive("dummy_rep")))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("description")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var count = type.Fields["description"]
            .Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public void InterfaceTypeExtension_RepeatableDirectiveOnArgument()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DummyQuery>()
            .AddType(new InterfaceType<IFoo>(t => t
                .Name("Foo")
                .Field(f => f.GetName(default!))
                .Argument("a", a => a
                    .Type<StringType>()
                    .Directive("dummy_rep", new ArgumentNode("a", "a")))))
            .AddType(new InterfaceTypeExtension(d => d
                .Name("Foo")
                .Field("name")
                .Argument("a", a =>
                    a.Directive("dummy_rep", new ArgumentNode("a", "b")))))
            .AddDirectiveType<RepeatableDummyDirective>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<InterfaceType>("Foo");
        var count = type.Fields["name"].Arguments["a"]
            .Directives["dummy_rep"]
            .Count();
        Assert.Equal(2, count);
    }

    public class DummyQuery
    {
        public string Foo { get; set; } = default!;
    }

    public class FooType : InterfaceType<IFoo>
    {
        protected override void Configure(
            IInterfaceTypeDescriptor<IFoo> descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field(t => t.Description);
        }
    }

    public class FooTypeExtension : InterfaceTypeExtension
    {
        protected override void Configure(
            IInterfaceTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Field("test")
                .Type<ListType<StringType>>();
        }
    }

    public interface IFoo
    {
        string Description { get; }

        string GetName(string a);
    }

    public class FooResolver
    {
        public string GetName2()
        {
            return "FooResolver.GetName2";
        }
    }

    public class DummyDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy");
            descriptor.Location(DirectiveLocation.Interface);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class DummyWithArgDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_arg");
            descriptor.Argument("a").Type<StringType>();
            descriptor.Location(DirectiveLocation.Interface);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }

    public class RepeatableDummyDirective : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy_rep");
            descriptor.Repeatable();
            descriptor.Argument("a").Type<StringType>();
            descriptor.Location(DirectiveLocation.Interface);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Location(DirectiveLocation.ArgumentDefinition);
        }
    }
}
