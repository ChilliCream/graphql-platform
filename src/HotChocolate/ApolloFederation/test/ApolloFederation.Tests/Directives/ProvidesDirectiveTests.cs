using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Directives;

public class ProvidesDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public async Task AnnotateProvidesToFieldCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
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
                    .Provides("name")
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
        Assert.Collection(
            testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(
                    FederationTypeNames.ProvidesDirective_Name,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"name\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotateProvidesToClassAttributePureCodeFirst()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(
            testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(
                    FederationTypeNames.ProvidesDirective_Name,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"name\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

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

        [Provides("name")]
        public Product Product { get; set; } = default!;
    }

    public class Product
    {
        public string Name { get; set; } = default!;
    }
}
