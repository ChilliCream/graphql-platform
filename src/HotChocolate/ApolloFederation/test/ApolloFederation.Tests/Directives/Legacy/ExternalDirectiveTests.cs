using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives.Legacy;

public class ExternalDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateExternalToTypeFieldCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType(o => o
                .Name("Query")
                .Field("entity")
                .Argument("id", a => a.Type<IntType>())
                .Type("User")
                .Resolve(_ => new { Id = 1 })
            )
            .AddObjectType(
                o =>
                {
                    o.Name("User")
                        .Key("id");
                    o.Field("id")
                        .Type<IntType>()
                        .Resolve(_ => 1);
                    o.Field("idCode")
                        .Type<StringType>()
                        .Resolve(_ => default!)
                        .External();
                })
            .BuildSchemaAsync();

        // act
        var query = schema.GetType<ObjectType>("User");

        // assert
        var directive = Assert.Single(query.Fields["idCode"].Directives);
        Assert.Equal(FederationTypeNames.ExternalDirective_Name, directive.Type.Name);

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateExternalToTypeFieldAnnotationBased()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var query = schema.GetType<ObjectType>("User");

        // assert
        var directive = Assert.Single(query.Fields["idCode"].Directives);
        Assert.Equal(FederationTypeNames.ExternalDirective_Name, directive.Type.Name);

        schema.MatchSnapshot();
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
