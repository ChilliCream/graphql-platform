using HotChocolate.Language;

namespace HotChocolate.Types;

public class InputObjectTypeExtensionTests
{
    [Fact]
    public void InputObjectTypeExtension_AddField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType<FooTypeExtension>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        Assert.True(type.Fields.ContainsField("test"));
    }

    [Fact]
    public void InputObjectTypeExtension_SetTypeContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        Assert.True(type.ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void InputObjectTypeExtension_SetFieldContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Field("description")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        Assert.True(type.Fields["description"]
            .ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void InputObjectTypeExtension_SetDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        Assert.True(type.Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void InputObjectTypeExtension_SetDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Field("name")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        Assert.True(type.Fields["name"]
            .Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void InputObjectTypeExtension_ReplaceDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new InputObjectType<Foo>(t => t
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        var value = type.Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InputObjectTypeExtension_ReplaceDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new InputObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Field("description")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        var value = type.Fields["description"].Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void InputObjectTypeExtension_RepeatableDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new InputObjectType<Foo>(t => t
                .Directive("dummy_rep")))
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        var count = type.Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    [Fact]
    public void InputObjectTypeExtension_RepeatableDirectiveOnField()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new InputObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Directive("dummy_rep")))
            .AddType(new InputObjectTypeExtension(d => d
                .Name("FooInput")
                .Field("description")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<InputObjectType>("FooInput");
        var count = type.Fields["description"]
            .Directives["dummy_rep"].Count();
        Assert.Equal(2, count);
    }

    public class QueryType
        : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field("description").Resolve("bar");
        }
    }

    public class FooType
        : InputObjectType<Foo>
    {
        protected override void Configure(
            IInputObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Description);
        }
    }

    public class FooTypeExtension
        : InputObjectTypeExtension
    {
        protected override void Configure(
            IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("FooInput");
            descriptor.Field("test")
                .Type<ListType<StringType>>();
        }
    }

    public class Foo
    {
        public Foo()
        {
        }

        public Foo(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; } = "hello";
        public string Description { get; } = "hello";
    }

    public class DummyDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy");
            descriptor.Location(DirectiveLocation.InputObject);
            descriptor.Location(DirectiveLocation.InputFieldDefinition);
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
            descriptor.Location(DirectiveLocation.InputObject);
            descriptor.Location(DirectiveLocation.InputFieldDefinition);
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
            descriptor.Location(DirectiveLocation.InputObject);
            descriptor.Location(DirectiveLocation.InputFieldDefinition);
        }
    }
}
