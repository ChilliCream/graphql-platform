using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public class TagDirectiveTests
{
    [Fact]
    public async Task EnsureAllLocationsAreApplied()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddObjectType<Foo>()
                .AddType<FooDirective>()
                .SetSchema(d => d.Tag("OnSchema"))
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_Tag()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(
                    """
                    type Query {
                        field: String @tag(name: "abc")
                    }

                    directive @tag("The name of the tag." name: String!)
                        repeatable on SCHEMA | SCALAR | OBJECT | FIELD_DEFINITION |
                            ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE |
                            INPUT_OBJECT | INPUT_FIELD_DEFINITION
                    """)
                .UseField(_ => _ => default)
                .BuildSchemaAsync();

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
                .BuildSchemaAsync());

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
        public string Bar { get; set; }
    }

    [DirectiveType(DirectiveLocation.Query)]
    public class FooDirective
    {
        [Tag("OnDirectiveArgument")]
        public string Arg { get; set; }
    }
}
