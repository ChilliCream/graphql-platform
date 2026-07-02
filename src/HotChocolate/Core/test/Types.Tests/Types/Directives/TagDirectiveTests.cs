using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public class TagDirectiveTests
{
    [Fact]
    public async Task SchemaFirst_Tag()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(
                    """
                    schema @tag(name: "OnSchema") {
                        query: Query
                    }

                    type Query @tag(name: "OnObjectType") {
                        foo(a: String! @tag(name: "OnObjectFieldArg")): IFoo!
                            @tag(name: "OnObjectField")
                        fooEnum(input: FooInput!): FooEnum!
                        fooUnion: FooUnion!
                    }

                    type Foo implements IFoo {
                        bar(baz: String!): String!
                    }

                    interface IFoo @tag(name: "OnInterface") {
                        bar(baz: String! @tag(name: "OnInterfaceFieldArg")): String!
                            @tag(name: "OnInterfaceField")
                    }

                    union FooUnion @tag(name: "OnUnion") = FooUnionA | FooUnionB

                    type FooUnionA {
                        id: String!
                    }

                    type FooUnionB {
                        id: String!
                    }

                    input FooInput @tag(name: "OnInputObjectType") {
                        bar: String! @tag(name: "OnInputObjectField")
                    }

                    enum FooEnum @tag(name: "OnEnum") {
                        FOO @tag(name: "OnEnumValue")
                        BAR
                    }

                    directive @foo(arg: String! @tag(name: "OnDirectiveArgument"))
                        @tag(name: "OnDirectiveDefinition") on QUERY

                    directive @tag("The name of the tag." name: String!)
                        repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION |
                            ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE |
                            INPUT_OBJECT | INPUT_FIELD_DEFINITION | DIRECTIVE_DEFINITION
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_Tag()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .SetSchema(d => d.Tag("OnSchema"))
                .AddQueryType(d =>
                {
                    d.Tag("OnObjectType");
                    d.Field("foo")
                        .Type("IFoo!")
                        .Argument("a", a => a.Type("String!").Tag("OnObjectFieldArg"))
                        .Tag("OnObjectField");
                    d.Field("fooEnum")
                        .Type("FooEnum!")
                        .Argument("input", a => a.Type("FooInput!"));
                    d.Field("fooUnion").Type("FooUnion!");
                })
                .AddInterfaceType(d =>
                {
                    d.Name("IFoo");
                    d.Tag("OnInterface");
                    d.Field("bar")
                        .Type("String!")
                        .Argument("baz", a => a.Type("String!").Tag("OnInterfaceFieldArg"))
                        .Tag("OnInterfaceField");
                })
                .AddObjectType(d => d
                    .Name("Foo")
                    .Implements("IFoo")
                    .Field("bar")
                    .Type("String!")
                    .Argument("baz", a => a.Type("String!")))
                .AddUnionType(d => d
                    .Name("FooUnion")
                    .Tag("OnUnion")
                    .Type(new NamedTypeNode("FooUnionA"))
                    .Type(new NamedTypeNode("FooUnionB")))
                .AddObjectType(d => d.Name("FooUnionA").Field("id").Type("String!"))
                .AddObjectType(d => d.Name("FooUnionB").Field("id").Type("String!"))
                .AddInputObjectType(d =>
                {
                    d.Name("FooInput");
                    d.Tag("OnInputObjectType");
                    d.Field("bar").Type("String!").Tag("OnInputObjectField");
                })
                .AddEnumType(d =>
                {
                    d.Name("FooEnum");
                    d.Tag("OnEnum");
                    d.Value("FOO").Tag("OnEnumValue");
                    d.Value("BAR");
                })
                .AddDirectiveType(d =>
                {
                    d.Name("foo");
                    d.Location(DirectiveLocation.Query);
                    d.Tag("OnDirectiveDefinition");
                    d.Argument("arg", a => a.Type("String!").Tag("OnDirectiveArgument"));
                })
                .UseField(_ => _ => default)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task ImplementationFirst_Tag()
    {
        // The SCHEMA position is intentionally omitted here: HotChocolate does not
        // apply class-level descriptor attributes to Schema subclasses, so a [Tag]
        // on a Schema class has no effect. Attribute-driven registration therefore
        // covers 12 of the 13 positions; the schema-first and code-first modes
        // cover all 13.

        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType<Foo>()
                .AddType<FooUnionA>()
                .AddType<FooUnionB>()
                .AddType<FooDirective>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task ValidNames()
    {
        var exception = await Record.ExceptionAsync(
            async () => await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(
                    """
                    type Query {
                        field: String
                            @tag(name: "tag")
                            @tag(name: "TAG")
                            @tag(name: "TAG_123")
                            @tag(name: "tag_123")
                            @tag(name: "tag-123")
                    }

                    directive @tag("The name of the tag." name: String!)
                        repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION |
                            ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE |
                            INPUT_OBJECT | INPUT_FIELD_DEFINITION
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("tag name")]
    [InlineData("tag*")]
    [InlineData("@TAG")]
    [InlineData("tag=name")]
    [InlineData("tagK")] // K = Kelvin Sign (U+212A)
    public async Task InvalidNames(string tagName)
    {
        async Task Act() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(
                    $$"""
                    type Query {
                        field: String @tag(name: "{{tagName}}")
                    }

                    directive @tag("The name of the tag." name: String!)
                        repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION |
                            ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE |
                            INPUT_OBJECT | INPUT_FIELD_DEFINITION
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

        Assert.Equal(
            "Tag names may only include alphanumeric characters (a-z, A-Z, 0-9), hyphens, and "
            + "underscores. (Parameter 'name')",
            (await Assert.ThrowsAsync<ArgumentException>(Act)).Message);
    }

    [Tag("OnObjectType")]
    public class Query
    {
        [Tag("OnObjectField")]
        public IFoo GetFoo([Tag("OnObjectFieldArg")] string a) => new Foo();

        public FooEnum GetFooEnum(FooInput input) => FooEnum.Foo;

        public IFooUnion GetFooUnion() => new FooUnionA();
    }

    [Tag("OnInterface")]
    public interface IFoo
    {
        [Tag("OnInterfaceField")]
        string Bar([Tag("OnInterfaceFieldArg")] string baz);
    }

    public class Foo : IFoo
    {
        public string Bar(string baz) => "Bar" + baz;
    }

    [UnionType("FooUnion")]
    [Tag("OnUnion")]
    public interface IFooUnion;

    public sealed class FooUnionA : IFooUnion
    {
        public string Id => "a";
    }

    public sealed class FooUnionB : IFooUnion
    {
        public string Id => "b";
    }

    [Tag("OnEnum")]
    public enum FooEnum
    {
        [Tag("OnEnumValue")]
        Foo,
        Bar
    }

    [Tag("OnInputObjectType")]
    public class FooInput
    {
        [Tag("OnInputObjectField")]
        public required string Bar { get; set; }
    }

    [Tag("OnDirectiveDefinition")]
    [DirectiveType(DirectiveLocation.Query)]
    public class FooDirective
    {
        [Tag("OnDirectiveArgument")]
        public required string Arg { get; set; }
    }
}
