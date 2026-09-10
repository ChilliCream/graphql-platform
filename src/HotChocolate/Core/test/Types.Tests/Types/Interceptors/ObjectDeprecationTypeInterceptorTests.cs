using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Interceptors;

public sealed class ObjectDeprecationTypeInterceptorTests
{
    [Fact]
    public void Build_Should_Fail_When_ObjectIsDeprecatedAndOptionIsOff()
    {
        // arrange
        var fooType = new ObjectType(t => t
            .Name("Foo")
            .Directive(new DirectiveNode(
                DirectiveNames.Deprecated.Name,
                new ArgumentNode(DirectiveNames.Deprecated.Arguments.Reason, "Use Bar.")))
            .Field("id")
            .Type<IdType>()
            .Resolve("1"));

        // act
        void Build() => TypeTestBase.CreateSchema(fooType);

        // assert
        var error = Assert.Single(Assert.Throws<SchemaException>(Build).Errors);
        Assert.Equal(
            "The specified directive `@deprecated` "
            + "is not allowed on the current location `Object`.",
            error.Message);
    }

    [Fact]
    public async Task Build_Should_Succeed_When_ObjectIsDeprecatedAndOptionIsOn()
    {
        // arrange
        const string sdl =
            """
            type Query { foo: Foo @deprecated(reason: "Use bar.") }

            type Foo @deprecated(reason: "Use Bar.") { id: ID }
            """;

        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(sdl)
            .UseField(next => next)
            .ModifyOptions(o => o.EnableObjectDeprecation = true)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var deprecated = schema.DirectiveTypes[DirectiveNames.Deprecated.Name];
        Assert.True(deprecated.Locations.HasFlag(DirectiveLocation.Object));
    }

    [Fact]
    public void Build_Should_ApplyDirective_When_ObjectIsDeprecatedAndOptionIsOn()
    {
        // arrange
        var fooType = new ObjectType(t => t
            .Name("Foo")
            .Directive(new DirectiveNode(
                DirectiveNames.Deprecated.Name,
                new ArgumentNode(DirectiveNames.Deprecated.Arguments.Reason, "Use Bar.")))
            .Field("id")
            .Type<IdType>()
            .Resolve("1"));

        // act
        TypeTestBase.CreateSchema(b => b
            .AddType(fooType)
            .ModifyOptions(o => o.EnableObjectDeprecation = true));

        // assert
        Assert.True(fooType.Directives.ContainsDirective(DirectiveNames.Deprecated.Name));
    }
}
