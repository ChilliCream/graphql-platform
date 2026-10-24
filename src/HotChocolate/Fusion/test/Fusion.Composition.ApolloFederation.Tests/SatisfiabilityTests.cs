using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class SatisfiabilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_ValueTypeFieldIsUnreachableThroughUnsharedRootField(
        bool unsharedRootFieldFirst)
    {
        // arrange
        var (firstRootField, secondRootField) = unsharedRootFieldFirst
            ? ("objectUnShared: Object!", "objectShared: Object! @shareable")
            : ("objectShared: Object! @shareable", "objectUnShared: Object!");

        var a =
            $$"""
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                {{firstRootField}}
                {{secondRootField}}
            }

            type Object {
                a: ID
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                objectShared: Object! @shareable
            }

            type Object {
                b: ID
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a), new SourceSchemaText("B", b)],
            new SchemaComposerOptions
            {
                Satisfiability = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true }
            },
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        string.Join(
            "\n\n",
            log.Where(e => e.Code == LogEntryCodes.UnsatisfiableQueryPath).Select(e => e.Message))
            .MatchInlineSnapshot(
                """
                Unable to access the field 'Object.b' on path 'A:Query.objectUnShared<Object>'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Object.b<ID>'.
                    No lookups found for type 'Object' in schema 'B'.
                """);
    }

    [Theory]
    [InlineData("A", "B")]
    [InlineData("B", "A")]
    public void Compose_Should_Succeed_When_SchemaLeavesThroughNonResolvableKey(
        string reviewsSchemaName,
        string productsSchemaName)
    {
        // arrange
        const string reviews =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                body: String!
                product: Product!
            }

            type Product @key(fields: "id", resolvable: false) {
                id: ID!
            }
            """;

        const string products =
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
            new SourceSchemaText(reviewsSchemaName, reviews),
            new SourceSchemaText(productsSchemaName, products));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_FieldRequiresEnteringSchemaThroughNonResolvableKey()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                body: String!
                product: Product!
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
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.reviewCount' on path 'B:Query.products<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.reviewCount<Int>'.
                No lookups found for type 'Product' in schema 'A'.
            """);
    }

    [Theory]
    [InlineData("A", "B")]
    [InlineData("B", "A")]
    public void Compose_Should_Succeed_When_ImplicitEntitiesHoldKeyFieldsInBothDirections(
        string productsSchemaName,
        string sellersSchemaName)
    {
        // arrange
        const string products =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
                seller: Seller!
            }

            type Seller {
                id: ID!
            }
            """;

        const string sellers =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Query {
                sellers: [Seller!]!
            }

            type Seller @key(fields: "id") {
                id: ID!
                rating: Int!
                topProduct: Product!
            }

            type Product {
                id: ID!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText(productsSchemaName, products),
            new SourceSchemaText(sellersSchemaName, sellers));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_ImplicitEntityLacksKeyFields()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String! @shareable
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                sellers: [Seller!]!
            }

            type Seller @key(fields: "id") {
                id: ID!
                topProduct: Product!
            }

            type Product {
                name: String! @shareable
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("A", a),
            new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.id' on path 'B:Query.sellers<Seller> -> B:Seller.topProduct<Product>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.id<ID>'.
                Unable to satisfy the requirement '{ id }' for lookup 'fusion__lookup_productById' in schema 'A'.
                  Unable to satisfy the requirement 'id'.
                    Unable to access the required field 'Product.id' on path 'B:Seller.topProduct<Product>'.
                      No other schemas contain the field 'Product.id'.
            """);
    }

    [Theory]
    [InlineData("extend type Product @key(fields: \"upc\")")]
    [InlineData("type Product @key(fields: \"upc\") @extends")]
    public void Compose_Should_Succeed_When_FederationV1StubAndV2SchemaContributeToSameEntity(
        string productStub)
    {
        // arrange
        const string products =
            """
            type Query {
                topProducts: [Product!]!
            }

            type Product @key(fields: "upc") {
                upc: String!
                name: String!
            }
            """;

        var reviews =
            $$"""
            extend type Query {
                latestReviews: [Review!]!
            }

            type Review {
                body: String!
                product: Product!
            }

            {{productStub}} {
                upc: String! @external
                reviews: [Review!]!
            }
            """;

        const string inventory =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Product @key(fields: "upc") {
                upc: String!
                inStock: Boolean!
            }
            """;

        var log = new CompositionLog();
        var options = CreateOptions();
        options.SourceSchemas["Products"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
        options.SourceSchemas["Reviews"] = new SourceSchemaOptions { IsApolloFederationV1 = true };
        var composer = new SchemaComposer(
            [
                new SourceSchemaText("Products", products),
                new SourceSchemaText("Reviews", reviews),
                new SourceSchemaText("Inventory", inventory)
            ],
            options,
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "B")]
    [InlineData("B", "A")]
    public void Compose_Should_Succeed_When_OneSchemaServesEverySelectionBelowSharedMutationField(
        string fullOrderSchemaName,
        string partialOrderSchemaName)
    {
        // arrange
        const string fullOrder =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@shareable"])

            type Query {
                orderCount: Int
            }

            type Mutation {
                placeOrder(sku: String!): Order! @shareable
            }

            type Order @shareable {
                id: ID!
                total: Int!
                status: String!
                lines: [OrderLine!]!
            }

            type OrderLine {
                sku: String!
                quantity: Int!
            }
            """;

        const string partialOrder =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@shareable"])

            type Mutation {
                placeOrder(sku: String!): Order! @shareable
            }

            type Order @shareable {
                id: ID!
                total: Int!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText(fullOrderSchemaName, fullOrder),
            new SourceSchemaText(partialOrderSchemaName, partialOrder));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(false, "a", "b")]
    [InlineData(true, "a", "b")]
    [InlineData(false, "A", "B")]
    [InlineData(true, "A", "B")]
    public void Compose_Should_Succeed_When_RequiresReadsKeylessSubgraphAlongSharedPath(
        bool importRequires,
        string requiringSchemaName,
        string otherSchemaName)
    {
        // arrange
        var imports = importRequires
            ? "\"@key\", \"@shareable\", \"@external\", \"@requires\""
            : "\"@key\", \"@shareable\", \"@external\"";

        var a =
            $$"""
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: [{{imports}}])

            type Query {
                entity: Entity! @shareable
            }

            type Entity @key(fields: "id") @shareable {
                id: ID
                age: Int! @external
                name: String! @requires(fields: "entity { age }")
                entity: Entity! @external
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable"])

            type Query {
                entity: Entity @shareable
            }

            type Entity @shareable {
                id: ID
                age: Int!
                entity: Entity!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText(requiringSchemaName, a),
            new SourceSchemaText(otherSchemaName, b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_TypeIsOnlyReachableThroughFieldThatIsExternalInTargetSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@shareable", "@external", "@requires"])

            type Query {
                e: E @shareable
            }

            type E @key(fields: "id") @shareable {
                id: ID!
                next: T @external
                r: String @requires(fields: "next { x }")
            }

            type T @shareable {
                x: String @external
                y: String
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable"])

            type Query {
                e: E @shareable
            }

            type E @shareable {
                id: ID!
                next: T
            }

            type T @shareable {
                x: String
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b));

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.y' on path 'A:Query.e<E> -> B:E.next<T>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:T.y<String>'.
                No lookups found for type 'T' in schema 'A'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TypeBelowExternalFieldHasKeyInRequiringSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@shareable", "@external", "@requires"])

            type Query {
                e: E @shareable
            }

            type E @key(fields: "id") @shareable {
                id: ID!
                next: T @external
                r: String @requires(fields: "next { x }")
            }

            type T @key(fields: "id") @shareable {
                id: ID!
                x: String @external
                y: String
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable"])

            type Query {
                e: E @shareable
            }

            type E @shareable {
                id: ID!
                next: T
            }

            type T @shareable {
                id: ID!
                x: String
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_KeylessEntityIsOnlyReachableThroughFieldThatIsExternalInTargetSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable", "@external", "@requires"])

            type Query {
                entity: Entity! @shareable
            }

            type Entity @shareable {
                id: ID
                age: Int! @external
                name: String! @requires(fields: "entity { age }")
                entity: Entity! @external
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@shareable"])

            type Query {
                entity: Entity @shareable
            }

            type Entity @shareable {
                id: ID
                age: Int!
                entity: Entity!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b));

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Entity.name' on path 'A:Query.entity<Entity> -> B:Entity.entity<Entity>'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Entity.name<String>'.
                No lookups found for type 'Entity' in schema 'A'.
            """
        ]);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("A", "B")]
    public void Compose_Should_Succeed_When_RequiresSelectsExternalKeyField(
        string requiringSchemaName,
        string otherSchemaName)
    {
        // arrange
        const string requiring =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Account @key(fields: "id") {
                id: ID! @external
                displayName: String! @requires(fields: "id")
            }
            """;

        const string other =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Query {
                accounts: [Account]
            }

            type Account @key(fields: "id") {
                id: ID!
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText(requiringSchemaName, requiring),
            new SourceSchemaText(otherSchemaName, other));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Fail_When_RequiresSelectsParentFieldServedByRequiringSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Query {
                orders: [Order]
            }

            type Order @key(fields: "id") {
                id: ID!
                customer: Customer
                price: Int @requires(fields: "customer { discount }")
            }

            type Customer @key(fields: "id") {
                id: ID!
                discount: Int @external
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Customer @key(fields: "id") {
                id: ID!
                discount: Int
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b));

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The @require directive on argument 'Order.price(customer:)' in schema 'a' specifies an invalid field selection against the composed schema.",
                "code": "REQUIRE_INVALID_FIELDS",
                "severity": "Error",
                "coordinate": "Order.price(customer:)",
                "member": "require",
                "schema": "a",
                "extensions": {
                    "errors": [
                        "The required field 'Order.customer' must not be served by the requiring source schema 'a'."
                    ]
                }
            }
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_RequiresSelectsExternalParentFieldServedByOtherSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Query {
                orders: [Order]
            }

            type Order @key(fields: "id") {
                id: ID!
                customer: Customer @external
                price: Int @requires(fields: "customer { discount }")
            }

            type Customer @key(fields: "id") {
                id: ID!
                discount: Int @external
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Order @key(fields: "id") {
                id: ID!
                customer: Customer
            }

            type Customer @key(fields: "id") {
                id: ID!
                discount: Int
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_CircularRequirementReadsParentFromThirdSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Query {
                feed: [Post]
            }

            type Post @key(fields: "id") {
                id: ID!
                byNovice: Boolean! @external
                byExpert: Boolean! @requires(fields: "byNovice")
            }

            type Author @key(fields: "id") {
                id: ID!
                name: String!
                yearsOfExperience: Int!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Post @key(fields: "id") {
                id: ID!
                author: Author!
            }

            type Author @key(fields: "id") {
                id: ID!
            }
            """;

        const string c =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Post @key(fields: "id") {
                id: ID!
                author: Author! @external
                byNovice: Boolean! @requires(fields: "author { yearsOfExperience }")
            }

            type Author @key(fields: "id") {
                id: ID!
                yearsOfExperience: Int! @external
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("a", a),
            new SourceSchemaText("b", b),
            new SourceSchemaText("c", c));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_RequiredFieldWithArgumentIsServedByOtherSubgraph()
    {
        // arrange
        const string c =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key"])

            type Query {
                feed: [Post]
            }

            type Post @key(fields: "id") {
                id: ID!
                comments(limit: Int!): [Comment]
            }

            type Comment @key(fields: "id") {
                id: ID!
                authorId: ID
                body: String
            }
            """;

        const string d =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.11", import: ["@key", "@external", "@requires"])

            type Post @key(fields: "id") {
                id: ID!
                comments(limit: Int!): [Comment] @external
                author: Author @requires(fields: "comments(limit: 3) { authorId }")
            }

            type Comment @key(fields: "id") {
                id: ID!
                authorId: ID @external
            }

            type Author {
                id: ID!
                name: String
            }
            """;

        // act
        var (result, log) = Compose(
            new SourceSchemaText("c", c),
            new SourceSchemaText("d", d));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
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
