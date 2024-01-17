using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.ApolloFederation.Directives;

public class ProvidesDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public void AddProvidesDirective_EnsureAvailableInSchema()
    {
        // arrange
        var schema = CreateSchema(b => b.AddDirectiveType<ProvidesDirectiveType>());

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal(WellKnownTypeNames.Provides));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<ProvidesDirectiveType>(directive);
        Assert.Equal(WellKnownTypeNames.Provides, directive!.Name);
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
                @"
                    type Review @key(fields: ""id"") {
                        id: Int!
                        product: Product! @provides(fields: ""name"")
                    }

                    type Product {
                        name: String!
                    }

                    type Query {
                        someField(a: Int): Review
                    }
                ")
            .AddDirectiveType<KeyDirectiveType>()
            .AddDirectiveType<ProvidesDirectiveType>()
            .AddType<FieldSetType>()
            .Use(_ => _ => default)
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(WellKnownTypeNames.Provides, providesDirective.Type.Name);
                Assert.Equal("fields", providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"name\"", providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateProvidesToFieldCodeFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddType(
                new ObjectType(o =>
                {
                    o.Name("Product");
                    o.Field("name").Type<StringType>();
                }))
            .AddType(
                new ObjectType(o =>
                {
                    o.Name("Review").Key("id");
                    o.Field("id").Type<IntType>();
                    ProvidesDescriptorExtensions.Provides(o.Field("product"), "name").Type("Product");
                }))
            .AddQueryType(
                new ObjectType(o =>
                {
                    o.Name("Query");
                    o.Field("someField")
                        .Argument("a", a => a.Type<IntType>())
                        .Type("Review");
                }))
            .AddType<FieldSetType>()
            .AddDirectiveType<KeyDirectiveType>()
            .AddDirectiveType<ProvidesDirectiveType>()
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
                    WellKnownTypeNames.Provides,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"name\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

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
        Assert.Collection(
            testType.Fields.Single(field => field.Name == "product").Directives,
            providesDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Provides,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "fields",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"name\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

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

        [Provides("name")]
        public Product Product { get; set; } = default!;
    }

    public class Product
    {
        public string Name { get; set; } = default!;
    }
}
