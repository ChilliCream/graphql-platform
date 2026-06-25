using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion;

public sealed class SchemaComposerTests
{
    [Fact]
    public void Compose_Should_Succeed_When_CursorFieldAndArgumentAreValid()
    {
        // arrange
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "Events",
                    """
                    type Query {
                        version: String
                    }

                    type Subscription {
                        onUserChanged(after: String @eventCursor): UserChangedEvent
                            @eventStream(message: "{ id changeType }")
                    }

                    type UserChangedEvent {
                        id: ID!
                        changeType: String!
                        cursor: String @eventCursor
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            new CompositionLog());

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        result.Value.MatchInlineSnapshot(
            """
            schema {
                query: Query
                subscription: Subscription
            }

            type Query @fusion__type(schema: EVENTS) {
                version: String @fusion__field(schema: EVENTS)
            }

            type Subscription @fusion__type(schema: EVENTS) {
                onUserChanged(after: String @fusion__inputField(schema: EVENTS)): UserChangedEvent
                    @fusion__field(schema: EVENTS)
                    @fusion__eventStream(
                        schema: EVENTS
                        message: "{ id changeType }"
                        cursorField: "cursor"
                        cursorArgument: "after"
                    )
            }

            type UserChangedEvent @fusion__type(schema: EVENTS) {
                changeType: String! @fusion__field(schema: EVENTS)
                cursor: String @fusion__field(schema: EVENTS)
                id: ID! @fusion__field(schema: EVENTS)
            }
            """);
    }

    [Fact]
    public void Compose_LookupFieldWithoutArguments_FailsWithLookupMustHaveArgumentsError()
    {
        // arrange
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        product: Product @lookup @internal
                    }

                    type Product {
                        id: ID!
                        name: String
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.LookupMustHaveArguments, entry.Code);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(
            "The lookup field 'Query.product' in schema 'A' must declare at least one argument.",
            entry.Message);
    }

    [Fact]
    public void Compose_OrphanedTypeAfterTagExclusion_DoesNotProduceShareableError()
    {
        // arrange
        // Schema A's Product is only reachable via Mutation. When Mutation is removed by
        // tag exclusion, Product becomes orphaned. Without pruning, Product.name would
        // collide with Schema B's Product.name and trigger an InvalidFieldSharing error.
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        book(id: ID!): Book @lookup
                    }

                    type Mutation @tag(name: "internal") {
                        createProduct(name: String!): Product
                    }

                    type Book {
                        id: ID!
                        title: String!
                    }

                    type Product {
                        id: ID!
                        name: String!
                    }

                    directive @tag(name: String!) repeatable on OBJECT
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID!
                        name: String!
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
            new CompositionLog());

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Types.ContainsName("Mutation"));
        Assert.True(result.Value.Types.ContainsName("Product"));
    }

    [Fact]
    public void Compose_WithExtensions_AppliesExtensions()
    {
        // arrange
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById1(id: ID!): Product
                        productById2(id: ID!): Product
                        lookups: InternalLookups!
                    }

                    type Product {
                        id: ID!
                        hidden: Int
                    }

                    type InternalLookups {
                        productBySku(sku: ID!): Product
                    }
                    """,
                    """
                    extend type Query {
                        productById1(id: ID!): Product @lookup
                        productById2(id: ID!): Product @internal
                        lookups: InternalLookups! @internal
                    }

                    extend type Product {
                        sku: String!
                        hidden: Int @inaccessible
                    }

                    extend type InternalLookups @internal {
                        productBySku(sku: ID!): Product @lookup
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            new CompositionLog());

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        result.Value.MatchInlineSnapshot(
            """
            schema {
                query: Query
            }

            type Query @fusion__type(schema: A) {
                productById1(id: ID! @fusion__inputField(schema: A)): Product
                    @fusion__field(schema: A)
            }

            type Product
                @fusion__type(schema: A)
                @fusion__lookup(
                    schema: A
                    key: "id"
                    field: "productById1(id: ID!): Product"
                    map: ["id"]
                    path: null
                    internal: false
                )
                @fusion__lookup(
                    schema: A
                    key: "sku"
                    field: "productBySku(sku: ID!): Product"
                    map: ["sku"]
                    path: "lookups"
                    internal: true
                ) {
                hidden: Int @fusion__field(schema: A) @fusion__inaccessible
                id: ID! @fusion__field(schema: A)
                sku: String! @fusion__field(schema: A)
            }
            """);
    }
}
