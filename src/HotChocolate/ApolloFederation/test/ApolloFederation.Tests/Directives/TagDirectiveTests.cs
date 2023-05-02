using System.Linq;
using HotChocolate.ApolloFederation.Constants;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.ApolloFederation.Directives;

public class TagDirectiveTests : FederationTypesTestBase
{
    [Fact]
    public void AddTagDirective_EnsureAvailableInSchema()
    {
        // arrange
        var schema = CreateSchema(b => b.AddDirectiveType<TagDirectiveType>());

        // act
        var directive =
            schema.DirectiveTypes.FirstOrDefault(
                t => t.Name.EqualsOrdinal(WellKnownTypeNames.Tag));

        // assert
        Assert.NotNull(directive);
        Assert.IsType<TagDirectiveType>(directive);
        Assert.Equal(WellKnownTypeNames.Tag, directive!.Name);
        Assert.Single(directive.Arguments);
        Assert.Equal(
            DirectiveLocation.FieldDefinition |
            DirectiveLocation.Interface |
            DirectiveLocation.Object |
            DirectiveLocation.Union |
            DirectiveLocation.ArgumentDefinition |
            DirectiveLocation.Scalar |
            DirectiveLocation.Enum |
            DirectiveLocation.EnumValue |
            DirectiveLocation.InputObject |
            DirectiveLocation.InputFieldDefinition, directive.Locations);
    }

    [Fact]
    public void AnnotateTagToFieldSchemaFirst()
    {
        // arrange
        Snapshot.FullName();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                @"
                    type Review @key(fields: ""id"") {
                        id: Int!
                        product: Product! @tag(name: ""public"")
                    }

                    type Product {
                        name: String!
                    }

                    type Query {
                        someField(a: Int): Review
                    }
                ")
            .AddDirectiveType<KeyDirectiveType>()
            .AddDirectiveType<TagDirectiveType>()
            .Use(_ => _ => default)
            .Create();

        // act
        var testType = schema.GetType<ObjectType>("Review");

        // assert
        Assert.Collection(testType.Fields.Single(field => field.Name == "product").Directives,
            tagDirective =>
            {
                Assert.Equal(WellKnownTypeNames.Tag, tagDirective.Type.Name);
                Assert.Equal("name", tagDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal("\"public\"", tagDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateTagToFieldCodeFirst()
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
                    o.Field("product").Type("Product").Tag("public");
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
            .AddDirectiveType<TagDirectiveType>()
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
                    WellKnownTypeNames.Tag,
                    providesDirective.Type.Name);
                Assert.Equal(
                    "name",
                    providesDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"public\"",
                    providesDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void AnnotateTagToClassAttributePureCodeFirst()
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
            tagDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Tag,
                    tagDirective.Type.Name);
                Assert.Equal(
                    "name",
                    tagDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"public\"",
                    tagDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        Assert.Collection(
            testType.Fields.Single(field => field.Name == "reviewSentiment").Directives,
            tagDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Tag,
                    tagDirective.Type.Name);
                Assert.Equal(
                    "name",
                    tagDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"public\"",
                    tagDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        var enumTestType = schema.GetType<EnumType>("ReviewSentiment");

        Assert.Collection(
            enumTestType.Directives,
            tagDirective =>
            {
                Assert.Equal(
                    WellKnownTypeNames.Tag,
                    tagDirective.Type.Name);
                Assert.Equal(
                    "name",
                    tagDirective.AsSyntaxNode().Arguments[0].Name.ToString());
                Assert.Equal(
                    "\"public\"",
                    tagDirective.AsSyntaxNode().Arguments[0].Value.ToString());
            });

        schema.ToString().MatchSnapshot();
    }

    public class Query
    {
        public Review SomeField(int id) => default!;
    }

    [Tag("public")]
    public enum ReviewSentiment
    {
        Negative,
        Positive
    }

    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Tag("public")]
        public Product Product { get; set; } = default!;

        [Tag("public")]
        public ReviewSentiment ReviewSentiment { get; set; }
    }

    public class Product
    {
        public string Name { get; set; } = default!;
    }
}
