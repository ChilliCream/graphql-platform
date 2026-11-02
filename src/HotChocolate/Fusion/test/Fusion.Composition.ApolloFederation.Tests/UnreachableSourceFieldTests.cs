using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class UnreachableSourceFieldTests
{
    [Fact]
    public void Compose_Should_Fail_When_NonResolvableEntityAddsFieldThatNoOtherSchemaResolves()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                topReviewer: String
            }

            type Product @key(fields: "id", resolvable: false) {
                id: ID!
                reviewCount: Int!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

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
    public void Compose_Should_Succeed_When_UnreachableNonResolvableEntityDeclaresOnlyKeyFields()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                topReviewer: String
            }

            type Product @key(fields: "id", resolvable: false) {
                id: ID!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_NonResolvableEntityIsReachableOnlyThroughInaccessibleField()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                topReviewer: String
            }

            type Product @key(fields: "id", resolvable: false) {
                id: ID!
                reviewCount: Int!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@inaccessible"])

            type Query {
                products: [Product!]! @inaccessible
                productCount: Int!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_SchemaWithoutKeyAddsFieldToEntityThatOnlyAnotherSchemaReturns()
    {
        // arrange
        const string catalog =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Author {
                name: String!
            }

            type Book @key(fields: "id") @shareable {
                id: ID!
                shelves: [Shelf!]!
            }

            type Shelf @key(fields: "id") @shareable {
                id: ID!
                books: [Book!]!
            }

            type Query @shareable {
                books: [Book!]!
            }
            """;

        const string authors =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Author @key(fields: "id") {
                id: ID!
            }

            type Book @key(fields: "id") @shareable {
                id: ID!
                author: Author!
            }

            type Shelf @key(fields: "id") @shareable {
                id: ID!
                books: [Book!]!
            }

            type Query @shareable {
                books: [Book!]!
            }
            """;

        const string pricing =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Book @key(fields: "id") @shareable {
                id: ID!
                price: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("catalog", catalog),
            new SourceSchemaText("authors", authors),
            new SourceSchemaText("pricing", pricing));

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Author.name' in schema 'catalog' cannot be resolved, because the type 'Author' is not reachable from the root types of schema 'catalog' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Author.name",
                "member": "name",
                "schema": "catalog",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_EntityFieldReturnsTypeWhoseFieldLivesInSchemaThatNeverReturnsIt()
    {
        // arrange
        const string catalog =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query @shareable {
                products: [Product!]!
            }

            type Product @key(fields: "id") @shareable {
                id: ID!
                sellers: [Seller!]!
                sellerPage: SellerPage!
            }

            type Seller @key(fields: "id") @shareable {
                id: ID!
                products: [Product!]!
                featuredProduct: Product!
            }

            type SellerPage {
                seller: Seller!
            }

            type Warehouse {
                address: String!
            }
            """;

        const string inventory =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query @shareable {
                products: [Product!]!
            }

            type Product @key(fields: "id") @shareable {
                id: ID!
                warehouse: Warehouse!
            }

            type Seller @key(fields: "id") @shareable {
                id: ID!
                products: [Product!]!
                featuredProduct: Product!
            }

            type Warehouse @key(fields: "id") {
                id: ID!
            }
            """;

        const string pricing =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Product @key(fields: "id") @shareable {
                id: ID!
                price: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("catalog", catalog),
            new SourceSchemaText("inventory", inventory),
            new SourceSchemaText("pricing", pricing));

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Warehouse.address' in schema 'catalog' cannot be resolved, because the type 'Warehouse' is not reachable from the root types of schema 'catalog' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Warehouse.address",
                "member": "address",
                "schema": "catalog",
                "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Fail_When_FederationV1ExtensionWithoutKeyAddsFieldToTypeItNeverReturns()
    {
        // arrange
        const string authors =
            """
            type Query {
                authors: [Author]
            }

            type Author {
                id: String!
                name: String!
            }
            """;

        const string awards =
            """
            extend type Author {
                awards: [Award]
            }

            type Award {
                id: String!
                title: String!
            }
            """;

        var options = CreateOptions();
        options.SourceSchemas["authors"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
        options.SourceSchemas["awards"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("authors", authors), new SourceSchemaText("awards", awards)],
            options,
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The field 'Author.awards' in schema 'awards' cannot be resolved, because the type 'Author' is not reachable from the root types of schema 'awards' and no other schema resolves the field.",
                "code": "UNREACHABLE_SOURCE_FIELD",
                "severity": "Error",
                "coordinate": "Author.awards",
                "member": "awards",
                "schema": "awards",
                "extensions": {}
            }
            """
        ]);
    }

    private static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) Compose(
        params SourceSchemaText[] sourceSchemas)
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(sourceSchemas, CreateOptions(), log);
        return (composer.Compose(), log);
    }

    private static SchemaComposerOptions CreateOptions()
        => new()
        {
            Satisfiability = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true }
        };
}
