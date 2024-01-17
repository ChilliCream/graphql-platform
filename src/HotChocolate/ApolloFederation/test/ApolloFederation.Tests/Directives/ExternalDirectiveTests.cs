using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.ApolloFederation.Directives;

public class ExternalDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public void AddExternalDirective_EnsureAvailableInSchema()
    {
        // arrange
        var schema = CreateSchema(b => b.AddDirectiveType<ExternalDirectiveType>());

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal(WellKnownTypeNames.External));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<ExternalDirectiveType>(directive);
        Assert.Equal(WellKnownTypeNames.External, directive!.Name);
        Assert.Empty(directive.Arguments);
        Assert.Equal(DirectiveLocation.FieldDefinition, directive.Locations);
    }

    [Fact]
    public void AnnotateExternalToTypeFieldCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddQueryType(o => o
                .Name("Query")
                .Field("field")
                .Argument("a", a => a.Type<StringType>())
                .Type<StringType>()
                .External())
            .AddDirectiveType<ExternalDirectiveType>()
            .Use(next => next)
            .Create();

        // act
        var query = schema.GetType<ObjectType>("Query");

        // assert
        Assert.Collection(
            query.Fields["field"].Directives,
            item => Assert.Equal(WellKnownTypeNames.External, item.Type.Name));
        schema.ToString().MatchSnapshot();
    }


    [Fact]
    public void AnnotateExternalToTypeFieldSchemaFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                    field(a: Int): String
                        @external
                }
                """)
            .AddDirectiveType<ExternalDirectiveType>()
            .Use(_ => _ => default)
            .Create();

        // act
        var queryInterface = schema.GetType<ObjectType>("Query");

        // assert
        Assert.Collection(
            queryInterface.Fields["field"].Directives,
            item => Assert.Equal(WellKnownTypeNames.External, item.Type.Name));
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateExternalToTypeFieldPureCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var query = schema.GetType<ObjectType>("User");

        // assert
        Assert.Collection(
            query.Fields["idCode"].Directives,
            item => Assert.Equal(WellKnownTypeNames.External, item.Type.Name));
        schema.ToString().MatchSnapshot();
    }
}

public class Query
{
    public User GetEntity(int id) => default!;
}

public class User
{
    [Key]
    public int Id { get; set; }
    [External]
    public string IdCode { get; set; } = default!;
}
