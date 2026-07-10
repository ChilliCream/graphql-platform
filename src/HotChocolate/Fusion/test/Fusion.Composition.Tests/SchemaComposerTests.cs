using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public sealed class SchemaComposerTests
{
    [Theory]
    [InlineData(NodeResolution.Gateway)]
    [InlineData(NodeResolution.SourceSchema)]
    public void Compose_Should_EmitExecutionMetadata(NodeResolution nodeResolution)
    {
        var options = new SchemaComposerOptions
        {
            Merger =
            {
                EnableGlobalObjectIdentification = nodeResolution is NodeResolution.SourceSchema,
                NodeResolution = nodeResolution
            }
        };
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", "type Query { ping: String }")],
            options,
            new CompositionLog());

        var result = composer.Compose();

        Assert.True(result.IsSuccess);
        var document = result.Value.ToSyntaxNode();
        var executionMetadataDefinitions = new DocumentNode(
        [
            document.Definitions.Single(
                definition => definition is EnumTypeDefinitionNode
                {
                    Name.Value: "fusion__NodeResolution"
                }),
            document.Definitions.Single(
                definition => definition is EnumTypeDefinitionNode
                {
                    Name.Value: "fusion__ShareableFieldRuntimeTypeRouting"
                }),
            document.Definitions.Single(
                definition => definition is DirectiveDefinitionNode
                {
                    Name.Value: "fusion__execution"
                })
        ]);

        executionMetadataDefinitions.ToString().MatchInlineSnapshot(
            """
            enum fusion__NodeResolution {
              GATEWAY
              SOURCE_SCHEMA
            }

            enum fusion__ShareableFieldRuntimeTypeRouting {
              SOURCE_LOCAL
              COMMON_RUNTIME_TYPES
            }

            directive @fusion__execution(
              nodeResolution: fusion__NodeResolution! = GATEWAY
              shareableFieldRuntimeTypeRouting: fusion__ShareableFieldRuntimeTypeRouting! = SOURCE_LOCAL
            ) on SCHEMA
            """);

        var schemaDefinition = document.Definitions
            .OfType<SchemaDefinitionNode>()
            .Single();
        var executionApplication = schemaDefinition.Directives.Single(
            directive => directive.Name.Value == "fusion__execution");

        executionApplication.ToString().MatchInlineSnapshot(
            nodeResolution is NodeResolution.Gateway
                ? """
                @fusion__execution(
                  nodeResolution: GATEWAY
                  shareableFieldRuntimeTypeRouting: SOURCE_LOCAL
                )
                """
                : """
                @fusion__execution(
                  nodeResolution: SOURCE_SCHEMA
                  shareableFieldRuntimeTypeRouting: SOURCE_LOCAL
                )
                """);
    }

    [Theory]
    [InlineData(ShareableFieldRuntimeTypeRouting.SourceLocal, "SOURCE_LOCAL")]
    [InlineData(ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes, "COMMON_RUNTIME_TYPES")]
    public void Compose_Should_EmitShareableFieldRuntimeTypeRouting(
        ShareableFieldRuntimeTypeRouting routing,
        string expectedValue)
    {
        var options = new SchemaComposerOptions
        {
            ApolloFederationCompatibility = { ShareableFieldRuntimeTypeRouting = routing }
        };
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", "type Query { ping: String }")],
            options,
            new CompositionLog());

        var result = composer.Compose();

        Assert.True(result.IsSuccess);
        var document = result.Value.ToSyntaxNode();
        var enumType = Assert.Single(
            document.Definitions.OfType<EnumTypeDefinitionNode>(),
            definition => definition.Name.Value == "fusion__ShareableFieldRuntimeTypeRouting");
        Assert.Equal(
            ["SOURCE_LOCAL", "COMMON_RUNTIME_TYPES"],
            enumType.Values.Select(value => value.Name.Value));
        var schemaDefinition = Assert.Single(document.Definitions.OfType<SchemaDefinitionNode>());
        var directive = Assert.Single(
            schemaDefinition.Directives,
            application => application.Name.Value == "fusion__execution");
        Assert.Equal(expectedValue, Assert.IsType<EnumValueNode>(
            directive.Arguments[1].Value).Value);
    }

    [Fact]
    public void Compose_Should_Fail_When_SourceSchemaNodeResolutionIsDisabled()
    {
        var log = new CompositionLog();
        var options = new SchemaComposerOptions
        {
            Merger = { NodeResolution = NodeResolution.SourceSchema }
        };
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", "type Query { ping: String }")],
            options,
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidNodeResolution, entry.Code);
        Assert.Equal(LogSeverity.Error, entry.Severity);
        Assert.Equal(
            "Source-schema node resolution requires global object identification to be enabled.",
            entry.Message);
    }

    [Fact]
    public void Compose_Should_Fail_When_NodeResolutionIsInvalid()
    {
        var log = new CompositionLog();
        var options = new SchemaComposerOptions
        {
            Merger = { NodeResolution = (NodeResolution)42 }
        };
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", "type Query { ping: String }")],
            options,
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidNodeResolution, entry.Code);
        Assert.Equal("The node resolution mode '42' is invalid.", entry.Message);
    }

    [Fact]
    public void Compose_Should_Fail_When_ShareableFieldRuntimeTypeRoutingIsInvalid()
    {
        var log = new CompositionLog();
        var options = new SchemaComposerOptions
        {
            ApolloFederationCompatibility =
            {
                ShareableFieldRuntimeTypeRouting = (ShareableFieldRuntimeTypeRouting)42
            }
        };
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", "type Query { ping: String }")],
            options,
            log);

        var result = composer.Compose();

        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.InvalidShareableFieldRuntimeTypeRouting, entry.Code);
        Assert.Equal(
            "The shareable field runtime type routing mode '42' is invalid.",
            entry.Message);
    }

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
                        topics: ["onUserChanged"]
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
    public void Compose_Should_Succeed_When_InvalidFieldDeprecationIsWarning()
    {
        // arrange
        // 'User.name' is deprecated but the 'Node.name' it implements is not, which the schema
        // validator reports as HCV0011. It defaults to a warning, so composition proceeds.
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    interface Node {
                        id: ID!
                        name: String
                    }

                    type User implements Node {
                        id: ID!
                        name: String @deprecated(reason: "Use fullName.")
                    }

                    type Query {
                        user: User
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Contains(log, e => e.Code == "HCV0011" && e.Severity == LogSeverity.Warning);
    }

    [Fact]
    public void Compose_Should_Fail_When_InvalidFieldDeprecationIsError()
    {
        // arrange
        // The same deprecation inconsistency, but the source schema opts into treating it as an
        // error, so composition fails.
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    interface Node {
                        id: ID!
                        name: String
                    }

                    type User implements Node {
                        id: ID!
                        name: String @deprecated(reason: "Use fullName.")
                    }

                    type Query {
                        user: User
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
                        InvalidFieldDeprecationSeverity = LogSeverity.Error
                    }
                }
            },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(log, e => e.Code == "HCV0011" && e.Severity == LogSeverity.Error);
    }

    [Fact]
    public void Compose_Should_Fail_When_SatisfiabilityEnabledAndSchemaUnsatisfiable()
    {
        // arrange
        // 'User' is reachable from both schemas, but neither exposes a lookup, so the field owned by
        // the other schema cannot be reached. Satisfiability validation is enabled by default.
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        profileById(id: ID!): Profile
                    }

                    type Profile {
                        id: ID!
                        user: User
                    }

                    type User {
                        id: ID! @shareable
                        name: String
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        orders: [Order]
                    }

                    type Order {
                        id: ID!
                        user: User
                    }

                    type User {
                        id: ID! @shareable
                        membershipStatus: String
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(log, e => e.Code == LogEntryCodes.UnsatisfiableQueryPath);
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
