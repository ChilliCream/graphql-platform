using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityCycleTests
{
    [Fact]
    public void Compose_Should_Fail_When_RequireFormsResolutionCycle()
    {
        // arrange
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        sku(description: String @require(field: "description")): String
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        productInBById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description(sku: String @require(field: "sku")): String
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.

            Unable to access the field 'Product.sku'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.
            """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ResolutionCycleHasNonCyclicEscape()
    {
        // arrange
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        sku(description: String @require(field: "description")): String
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        productInBById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description(sku: String @require(field: "sku")): String @shareable
                    }
                    """),
                new SourceSchemaText(
                    "C",
                    """
                    type Query {
                        productInCById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description: String @shareable
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "C", true)]
    [InlineData("A", "C", false)]
    [InlineData("C", "A", true)]
    [InlineData("C", "A", false)]
    public void Validate_Should_Succeed_When_EntityChainCyclesThroughThreeSchemas(
        string authorsSchemaName,
        string publishersSchemaName,
        bool nameFirst)
    {
        // arrange
        var (firstAuthorField, secondAuthorField) = nameFirst
            ? ("name: String", "latestBook: Book")
            : ("latestBook: Book", "name: String");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                authorsSchemaName,
                $$"""
                type Query {
                    authors: [Author]
                    authorById(id: ID!): Author @lookup @internal
                }

                type Author {
                    id: ID! @shareable
                    {{firstAuthorField}}
                    {{secondAuthorField}}
                }

                type Book {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    bookById(id: ID!): Book @lookup @internal
                }

                type Book {
                    id: ID! @shareable
                    title: String
                    publisher: Publisher
                }

                type Publisher {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                publishersSchemaName,
                """
                type Query {
                    publisherById(id: ID!): Publisher @lookup @internal
                }

                type Publisher {
                    id: ID! @shareable
                    name: String
                    leadAuthor: Author
                }

                type Author {
                    id: ID! @shareable
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_Should_Fail_When_EntityChainCyclesBackToFirstSchemaWithoutLookup(bool idFirst)
    {
        // arrange
        var sourceSchemas = CreateCycleWithoutLookupSchemas(
            authorsSchemaName: "A",
            publishersSchemaName: "C",
            idFirst);

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Author.latestBook' on path 'A:Query.authors<Author> -> A:Author.latestBook<Book> -> B:Book.publisher<Publisher> -> C:Publisher.leadAuthor<Author>'.
              Unable to transition between schemas 'C' and 'A' for access to field 'A:Author.latestBook<Book>'.
                No lookups found for type 'Author' in schema 'A'.
            """);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_Should_Fail_When_EntityChainCyclesBackToLastSchemaWithoutLookup(bool idFirst)
    {
        // arrange
        var sourceSchemas = CreateCycleWithoutLookupSchemas(
            authorsSchemaName: "C",
            publishersSchemaName: "A",
            idFirst);

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Author.latestBook' on path 'C:Query.authors<Author> -> C:Author.latestBook<Book> -> B:Book.publisher<Publisher> -> A:Publisher.leadAuthor<Author>'.
              Unable to transition between schemas 'A' and 'C' for access to field 'C:Author.latestBook<Book>'.
                No lookups found for type 'Author' in schema 'C'.
            """);
    }

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_EntityFieldReturnsItsOwnTypeInEachSchema(
        string managersSchemaName,
        string mentorsSchemaName,
        bool selfReferenceFirst)
    {
        // arrange
        var (firstManagerField, secondManagerField) = selfReferenceFirst
            ? ("manager: Employee", "name: String")
            : ("name: String", "manager: Employee");
        var (firstMentorField, secondMentorField) = selfReferenceFirst
            ? ("mentor: Employee", "title: String")
            : ("title: String", "mentor: Employee");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                managersSchemaName,
                $$"""
                type Query {
                    employees: [Employee]
                    employeeById(id: ID!): Employee @lookup @internal
                }

                type Employee {
                    id: ID! @shareable
                    {{firstManagerField}}
                    {{secondManagerField}}
                }
                """),
            new SourceSchemaText(
                mentorsSchemaName,
                $$"""
                type Query {
                    employeeById(id: ID!): Employee @lookup @internal
                }

                type Employee {
                    id: ID! @shareable
                    {{firstMentorField}}
                    {{secondMentorField}}
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_SharedEntityFieldReturnsItsOwnType(
        string catalogSchemaName,
        string pricingSchemaName,
        bool selfReferenceFirst)
    {
        // arrange
        var (firstCatalogField, secondCatalogField) = selfReferenceFirst
            ? ("similar: [Product] @shareable", "name: String")
            : ("name: String", "similar: [Product] @shareable");
        var (firstPricingField, secondPricingField) = selfReferenceFirst
            ? ("similar: [Product] @shareable", "price: Int")
            : ("price: Int", "similar: [Product] @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                catalogSchemaName,
                $$"""
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID! @shareable
                    {{firstCatalogField}}
                    {{secondCatalogField}}
                }
                """),
            new SourceSchemaText(
                pricingSchemaName,
                $$"""
                type Query {
                    topProducts: [Product]
                    productById(id: ID!): Product @lookup @internal
                }

                type Product {
                    id: ID! @shareable
                    {{firstPricingField}}
                    {{secondPricingField}}
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData("A", "B", true)]
    [InlineData("A", "B", false)]
    [InlineData("B", "A", true)]
    [InlineData("B", "A", false)]
    public void Validate_Should_Succeed_When_SharedValueTypeFieldReturnsItsOwnType(
        string catalogSchemaName,
        string pricingSchemaName,
        bool selfReferenceFirst)
    {
        // arrange
        var (firstCatalogField, secondCatalogField) = selfReferenceFirst
            ? ("similar: [Product] @shareable", "name: String")
            : ("name: String", "similar: [Product] @shareable");
        var (firstPricingField, secondPricingField) = selfReferenceFirst
            ? ("similar: [Product] @shareable", "price: Int")
            : ("price: Int", "similar: [Product] @shareable");

        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                catalogSchemaName,
                $$"""
                type Query {
                    product: Product @shareable
                }

                type Product {
                    {{firstCatalogField}}
                    {{secondCatalogField}}
                }
                """),
            new SourceSchemaText(
                pricingSchemaName,
                $$"""
                type Query {
                    product: Product @shareable
                }

                type Product {
                    {{firstPricingField}}
                    {{secondPricingField}}
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    private static SourceSchemaText[] CreateCycleWithoutLookupSchemas(
        string authorsSchemaName,
        string publishersSchemaName,
        bool idFirst)
    {
        var (firstAuthorField, secondAuthorField) = idFirst
            ? ("id: ID! @shareable", "latestBook: Book")
            : ("latestBook: Book", "id: ID! @shareable");

        return
        [
            new SourceSchemaText(
                authorsSchemaName,
                $$"""
                type Query {
                    authors: [Author]
                }

                type Author {
                    {{firstAuthorField}}
                    {{secondAuthorField}}
                }

                type Book {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    bookById(id: ID!): Book @lookup @internal
                }

                type Book {
                    id: ID! @shareable
                    title: String
                    publisher: Publisher
                }

                type Publisher {
                    id: ID! @shareable
                }
                """),
            new SourceSchemaText(
                publishersSchemaName,
                """
                type Query {
                    publisherById(id: ID!): Publisher @lookup @internal
                }

                type Publisher {
                    id: ID! @shareable
                    name: String
                    leadAuthor: Author
                }

                type Author {
                    id: ID! @shareable
                }
                """)
        ];
    }

    [Fact]
    public void Compose_Should_Fail_When_RequirementRepeatsFieldOfItsPath()
    {
        // arrange
        // The requirement selects T.next twice in A, so its path holds the same item twice.
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "a",
                """
                type Query {
                    t: T
                }

                type T {
                    id: ID! @shareable
                    next: T @shareable
                    f(x: String @require(field: "n.next.next.x")): String
                }
                """),
            new SourceSchemaText(
                "b",
                """
                type Query {
                    tById(id: ID!): T @lookup @internal
                }

                type T {
                    id: ID! @shareable
                    n: T
                    next: T @shareable
                    x: String
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.False(result.IsSuccess);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'T.f' on path 'A:Query.t<T>'.
              Unable to satisfy the requirement '{ n { next { next { x } } } }' on field 'A:T.f<String>'.
                The field 'T.f' in schema 'A' requires data from another schema, but schema 'A' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """,
            """
            Unable to access the field 'T.f' on path 'A:Query.t<T> -> B:T.next<T>'.
              Unable to satisfy the requirement '{ n { next { next { x } } } }' on field 'A:T.f<String>'.
                The field 'T.f' in schema 'A' requires data from another schema, but schema 'A' has no lookup for type 'T' that could resolve the field once the required data has been fetched.
            """
        ]);
    }
}
