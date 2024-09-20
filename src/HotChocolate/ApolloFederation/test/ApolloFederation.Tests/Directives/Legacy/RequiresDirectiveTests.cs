using CookieCrumble;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives.Legacy;

public class RequiresDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateProvidesToFieldCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddObjectType(o =>
            {
                o.Name("Product");
                o.Field("name")
                    .Type<StringType>()
                    .Resolve(_ => default!);
            })
            .AddObjectType(o =>
            {
                o.Name("Review").Key("id");
                o.Field("id")
                    .Type<IntType>()
                    .Resolve(_ => default!);
                o.Field("product")
                    .Requires("id")
                    .Type("Product")
                    .Resolve(_ => default!);
            })
            .AddQueryType(o =>
            {
                o.Name("Query");
                o.Field("someField")
                    .Argument("a", a => a.Type<IntType>())
                    .Type("Review")
                    .Resolve(_ => default!);
            })
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        var requiresDirective = Assert.Single(testType.Fields.Single(field => field.Name == "product").Directives);
        var directiveNode = requiresDirective.AsSyntaxNode();
        Assert.Equal(FederationTypeNames.RequiresDirective_Name, requiresDirective.Type.Name);
        Assert.Equal("fields", directiveNode.Arguments[0].Name.ToString());
        Assert.Equal("\"id\"", directiveNode.Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateProvidesToClassAttributePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation10)
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        var requiresDirective = Assert.Single(testType.Fields.Single(field => field.Name == "product").Directives);
        var directiveNode = requiresDirective.AsSyntaxNode();
        Assert.Equal(FederationTypeNames.RequiresDirective_Name, requiresDirective.Type.Name);
        Assert.Equal("fields", directiveNode.Arguments[0].Name.ToString());
        Assert.Equal("\"id\"", directiveNode.Arguments[0].Value.ToString());

        schema.MatchSnapshot();
    }

    public class Query
    {
        public Review SomeField(int id) => default!;
    }

    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Requires("id")]
        public Product Product { get; set; } = default!;
    }

    public class Product
    {
        public string Name { get; set; } = default!;
    }
}
