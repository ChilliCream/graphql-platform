using HotChocolate.Execution;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Directives;

public static class SpecifiedByDirectiveTypeTests
{
    [Fact]
    public static async Task EnsureSpecifiedByDirectiveExistsInSdl()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query1>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task EnsureSpecifiedByDirectiveExists()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query1>()
                .BuildSchemaAsync();

        Assert.Contains(
            schema.DirectiveTypes,
            t => t.Name.EqualsOrdinal(DirectiveNames.SpecifiedBy.Name));

        Assert.Empty(
            schema.Types.GetType<ScalarType>("DateTime").Directives);

        Assert.NotNull(
            schema.Types.GetType<ScalarType>("DateTime").SpecifiedBy);
    }

    [Fact]
    public static async Task ReadSchemaWithSpecifiedByDirectiveDeclared()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    schema {
                      query: Query1
                    }

                    type Query1 {
                      date: DateTime!
                    }

                    "The `@specifiedBy` directive is used within the type system definition language to provide a URL for specifying the behavior of custom scalar definitions."
                    directive @specifiedBy("The specifiedBy URL points to a human-readable specification. This field will only read a result for scalar types." url: String!) on SCALAR

                    "The `DateTime` scalar type represents a date and time with time zone offset information."
                    scalar DateTime @specifiedBy(url: "https://scalars.graphql.org/chillicream/date-time.html")
                    """)
                .UseField(next => next)
                .BuildSchemaAsync();

        Assert.Contains(
            schema.DirectiveTypes,
            t => t.Name.EqualsOrdinal(DirectiveNames.SpecifiedBy.Name));

        Assert.Empty(
            schema.Types.GetType<ScalarType>("DateTime").Directives);

        Assert.NotNull(
            schema.Types.GetType<ScalarType>("DateTime").SpecifiedBy);
    }

    [Fact]
    public static async Task ReadSchemaWithSpecifiedByDirectiveNotDeclared()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(
                    """
                    schema {
                      query: Query1
                    }

                    type Query1 {
                      date: DateTime!
                    }

                    "The `@specifiedBy` directive is used within the type system definition language to provide a URL for specifying the behavior of custom scalar definitions."
                    directive @specifiedBy("The specifiedBy URL points to a human-readable specification. This field will only read a result for scalar types." url: String!) on SCALAR
                    """)
                .UseField(next => next)
                .BuildSchemaAsync();

        Assert.Contains(
            schema.DirectiveTypes,
            t => t.Name.EqualsOrdinal(DirectiveNames.SpecifiedBy.Name));

        Assert.Empty(
            schema.Types.GetType<ScalarType>("DateTime").Directives);

        Assert.NotNull(
            schema.Types.GetType<ScalarType>("DateTime").SpecifiedBy);
    }

    public class Query1
    {
        public DateTime GetDate() => DateTime.Now;
    }
}
