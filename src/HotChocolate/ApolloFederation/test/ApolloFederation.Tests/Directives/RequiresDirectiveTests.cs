using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.ApolloFederation.Directives;

public class RequiresDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public void AddRequiresDirective_EnsureAvailableInSchema()
    {
        // arrange
        var schema = CreateSchema(b => b.AddDirectiveType<RequiresDirectiveType>());

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal(WellKnownTypeNames.Requires));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<RequiresDirectiveType>(directive);
        Assert.Equal(WellKnownTypeNames.Requires, directive!.Name);
        Assert.Single(directive.Arguments);
        AssertDirectiveHasFieldsArgument(directive.Arguments);
        Assert.Equal(DirectiveLocation.FieldDefinition, directive.Locations);
    }

    [Fact]
    public void AnnotateProvidesToFieldSchemaFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"type Review @key(fields: ""id"") {
                        id: Int!
                        product: Product! @requires(fields: ""id"")
                    }

                    type Product {
                        name: String!
                    }

                    type Query {
                        someField(a: Int): Review
                    }")
            .AddDirectiveType<KeyDirectiveType>()
            .AddDirectiveType<RequiresDirectiveType>()
            .AddType<FieldSetType>()
            .Use(_ => _ => default)
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(
            testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Requires,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"id\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateProvidesToFieldCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var productType = new ObjectType(
            o =>
            {
                o.Name("Product");
                o.Field("name").Type<StringType>();
            });

        var reviewType = new ObjectType(
            o =>
            {
                o.Name("Review").Key("id");
                o.Field("id").Type<IntType>();
                o.Field("product").Requires("id").Type(productType);
            });

        var queryType = new ObjectType(
            o =>
            {
                o.Name("Query");
                o.Field("someField").Argument("a", a => a.Type<IntType>()).Type(reviewType);
            });

        var schema = SchemaBuilder.New()
            .AddQueryType(queryType)
            .AddType<FieldSetType>()
            .AddDirectiveType<KeyDirectiveType>()
            .AddDirectiveType<RequiresDirectiveType>()
            .Use(_ => _ => default)
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                var directiveNode = providesDirective.AsSyntaxNode();
                Assert.Equal(WellKnownTypeNames.Requires, providesDirective.Type.Name);
                Assert.Equal("fields", directiveNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", directiveNode.Arguments[0].Value.ToString());
            }
        );
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateProvidesToClassAttributePureCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                var directiveNode = providesDirective.AsSyntaxNode();
                Assert.Equal(WellKnownTypeNames.Requires, providesDirective.Type.Name);
                Assert.Equal("fields", directiveNode.Arguments[0].Name.ToString());
                Assert.Equal("\"id\"", directiveNode.Arguments[0].Value.ToString());
            }
        );
        schema.ToString().MatchSnapshot();
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
