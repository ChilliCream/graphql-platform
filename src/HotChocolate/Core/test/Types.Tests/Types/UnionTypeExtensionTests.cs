using HotChocolate.Language;

namespace HotChocolate.Types;

public class UnionTypeExtensionTests
{
    [Fact]
    public void UnionTypeExtension_AddType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType<FooTypeExtension>()
            .Create();

        // assert
        var type = schema.GetType<FooType>("Foo");
        Assert.Collection(type.Types.Values,
            t => Assert.IsType<AType>(t),
            t => Assert.IsType<BType>(t));
    }

    [Fact]
    public void UnionTypeExtension_SetTypeContextData()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new UnionTypeExtension(d => d
                .Name("Foo")
                .Extend()
                .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
            .Create();

        // assert
        var type = schema.GetType<UnionType>("Foo");
        Assert.True(type.ContextData.ContainsKey("foo"));
    }

    [Fact]
    public void UnionTypeExtension_SetDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new UnionTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy")))
            .AddDirectiveType<DummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<UnionType>("Foo");
        Assert.True(type.Directives.ContainsDirective("dummy"));
    }

    [Fact]
    public void UnionTypeExtension_ReplaceDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new UnionType(t => t
                .Name("Foo")
                .Type<AType>()
                .Directive("dummy_arg", new ArgumentNode("a", "a"))))
            .AddType(new UnionTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_arg", new ArgumentNode("a", "b"))))
            .AddDirectiveType<DummyWithArgDirective>()
            .Create();

        // assert
        var type = schema.GetType<UnionType>("Foo");
        var value = type.Directives["dummy_arg"]
            .First().GetArgumentValue<string>("a");
        Assert.Equal("b", value);
    }

    [Fact]
    public void UnionTypeExtension_CopyDependencies_ToType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType<FooType>()
            .AddType(new UnionTypeExtension(d => d
                .Name("Foo")
                .Type<BType>()))
            .Create();

        // assert
        var type = schema.GetType<FooType>("Foo");
        Assert.Collection(type.Types.Values,
            t => Assert.IsType<AType>(t),
            t => Assert.IsType<BType>(t));
    }

    [Fact]
    public void UnionTypeExtension_RepeatableDirectiveOnType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryType>()
            .AddType(new UnionType(t => t
                .Name("Foo")
                .Type<AType>()
                .Directive("dummy_rep")))
            .AddType(new UnionTypeExtension(d => d
                .Name("Foo")
                .Directive("dummy_rep")))
            .AddDirectiveType<RepeatableDummyDirective>()
            .Create();

        // assert
        var type = schema.GetType<UnionType>("Foo");
        var count = type.Directives["dummy_rep"].Count();
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

    public class AType
        : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("A");
            descriptor.Field("description").Resolve("bar");
        }
    }

    public class BType
        : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("B");
            descriptor.Field("description").Resolve("bar");
        }
    }

    public class FooType
        : UnionType
    {
        protected override void Configure(
            IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Type<AType>();
        }
    }

    public class FooTypeExtension
        : UnionTypeExtension
    {
        protected override void Configure(
            IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("Foo");
            descriptor.Type<BType>();
        }
    }

    public class Foo
    {
        public string Description { get; } = "hello";

        public string GetName(string a)
        {
            return null;
        }
    }

    public class DummyDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("dummy");
            descriptor.Location(DirectiveLocation.Union);
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
            descriptor.Location(DirectiveLocation.Union);
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
            descriptor.Location(DirectiveLocation.Union);
        }
    }
}
