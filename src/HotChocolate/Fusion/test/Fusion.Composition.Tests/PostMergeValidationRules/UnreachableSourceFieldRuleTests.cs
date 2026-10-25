using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class UnreachableSourceFieldRuleTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("C")]
    public void Compose_Should_Fail_When_ValueTypeFieldIsDeclaredInSchemaThatNeverReturnsType(
        string usersSchemaName)
    {
        // arrange
        var a = new SourceSchemaText(
            usersSchemaName,
            """
            type Query {
                users: [User]
            }

            type User {
                id: ID!
                name: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                userCount: Int
            }

            type User {
                id: ID!
                email: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'User.email' in schema 'B' cannot be resolved, because the type 'User' is not reachable from the root types of schema 'B' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "User.email",
                "member": "email",
                "schema": "B",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_ImplementationTypeAddsFieldWithoutLookupOrRootPath()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                media: [Media]
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                title: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                bookCount: Int
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                pageCount: Int
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Book.pageCount' in schema 'B' cannot be resolved, because the type 'Book' is not reachable from the root types of schema 'B' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Book.pageCount",
                "member": "pageCount",
                "schema": "B",
                "extensions": {}
            }
            """
        ]);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("C")]
    public void Compose_Should_Succeed_When_EveryImplementationResolvesFieldOfUnreachableInterface(
        string mediaSchemaName)
    {
        // arrange
        var a = new SourceSchemaText(
            mediaSchemaName,
            """
            type Query {
                media: [Media]
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                title: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                mediaCount: Int
            }

            interface Media {
                id: ID!
                title: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_ImplementationDoesNotResolveFieldOfUnreachableInterface()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                media: [Media]
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                title: String
            }

            type Movie implements Media {
                id: ID!
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                mediaCount: Int
            }

            interface Media {
                id: ID!
                title: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Media.title' in schema 'B' cannot be resolved, because the type 'Media' is not reachable from the root types of schema 'B' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Media.title",
                "member": "title",
                "schema": "B",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_SharedRootFieldTypeHasFieldInSchemaThatNeverReturnsType()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                viewer: Viewer @shareable
            }

            type Viewer @shareable {
                id: ID!
                name: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                viewer: Viewer @shareable
            }

            type Viewer @shareable {
                id: ID!
                name: String
            }
            """);

        var c = new SourceSchemaText(
            "C",
            """
            type Query {
                avatarCount: Int
            }

            type Viewer {
                avatarUrl: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b, c]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Viewer.avatarUrl' in schema 'C' cannot be resolved, because the type 'Viewer' is not reachable from the root types of schema 'C' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Viewer.avatarUrl",
                "member": "avatarUrl",
                "schema": "C",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_OverridingFieldIsDeclaredOnUnreachableType()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                productCount: Int
            }

            type Product {
                id: ID!
                name: String @override(from: "A")
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Product.name' in schema 'B' cannot be resolved, because the type 'Product' is not reachable from the root types of schema 'B' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Product.name",
                "member": "name",
                "schema": "B",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_ExcludeByTagLeavesFieldWithoutResolvingSchema()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        books: [Book]
                        productById(id: ID!): Product @lookup @tag(name: "internal")
                    }

                    type Book {
                        id: ID!
                        title: String
                    }

                    type Product {
                        id: ID!
                        reviewCount: Int
                    }

                    directive @tag(name: String!) repeatable on FIELD_DEFINITION
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        products: [Product]
                    }

                    type Product {
                        id: ID!
                        name: String
                    }
                    """)
            ],
            new SchemaComposerOptions
            {
                Merger = { AddFusionDefinitions = false },
                SourceSchemas =
                {
                    ["A"] = new SourceSchemaOptions
                    {
                        Preprocessor = new SourceSchemaPreprocessorOptions
                        {
                            ExcludeByTag = ["internal"]
                        }
                    }
                }
            },
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Product.reviewCount' in schema 'A' cannot be resolved, because the type 'Product' is not reachable from the root types of schema 'A' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Product.reviewCount",
                "member": "reviewCount",
                "schema": "A",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_UnreachableTypeIsAlsoUnreachableInCompositeSchema()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                books: [Book]
            }

            type Book {
                id: ID!
                title: String
            }

            type Draft {
                id: ID!
                note: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                authors: [Author]
            }

            type Author {
                id: ID!
                name: String
            }

            type Draft {
                id: ID!
                revision: Int
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        Assert.False(result.Value.Types.ContainsName("Draft"));
    }

    [Fact]
    public void Compose_Should_Succeed_When_UnreachableTypeIsReachableOnlyThroughInaccessibleField()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                reviewCount: Int
            }

            type Product @shareable {
                id: ID!
                rating: Int
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                products: [Product] @inaccessible
                productCount: Int
            }

            type Product @shareable {
                id: ID!
                name: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_UnreachableTypeIsInaccessibleInCompositeSchema()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                reviewCount: Int
            }

            type Product @shareable {
                id: ID!
                rating: Int
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                products: [Product] @inaccessible
                productCount: Int
            }

            type Product @inaccessible @shareable {
                id: ID!
                name: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_UnreachableTypeIsInaccessibleImplementationInCompositeSchema()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                reviewCount: Int
            }

            type Product @shareable {
                id: ID!
                rating: Int
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                media: [Media]
            }

            interface Media {
                id: ID!
            }

            type Book implements Media {
                id: ID!
                title: String
            }

            type Product implements Media @inaccessible @shareable {
                id: ID!
                name: String
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_AnotherSchemaResolvesEveryFieldOfUnreachableType()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                books: [Book]
            }

            type Book {
                id: ID!
                title: String
            }

            type Product {
                id: ID!
                name: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID!
                name: String
                price: Int
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_UnresolvedFieldsOfUnreachableTypeAreInternalOrInaccessible()
    {
        // arrange
        var a = new SourceSchemaText(
            "A",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """);

        var b = new SourceSchemaText(
            "B",
            """
            type Query {
                productCount: Int
            }

            type Product {
                id: ID!
                secretCode: String @internal
                legacyCode: String @inaccessible
            }
            """);

        // act
        var (result, log) = ComposeSourceSchemas([a, b]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }
}
