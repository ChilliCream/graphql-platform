namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerMcpToolAnnotationsDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @mcpToolAnnotations directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_McpToolAnnotationsDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field: Int @mcpToolAnnotations(openWorldHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    field: Int @mcpToolAnnotations(openWorldHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @mcpToolAnnotations(openWorldHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            directive @mcpToolAnnotations(destructiveHint: Boolean idempotentHint: Boolean openWorldHint: Boolean) on FIELD_DEFINITION
            """);
    }

    // Do not merge @mcpToolAnnotations directives when the definitions do not match the canonical
    // definition.
    [Fact]
    public void Merge_McpToolAnnotationsDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field: Int @mcpToolAnnotations(anotherHint: 1)
                }

                directive @mcpToolAnnotations(anotherHint: Int) repeatable on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    field: Int @mcpToolAnnotations
                }

                directive @mcpToolAnnotations on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_McpToolAnnotationsDirectivesAllArgumentsNull_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    field1: Int @mcpToolAnnotations
                    field2: Int @mcpToolAnnotations(destructiveHint: null, idempotentHint: null, openWorldHint: null)
                }

                type Mutation {
                    field1: Int @mcpToolAnnotations
                    field2: Int @mcpToolAnnotations(destructiveHint: null, idempotentHint: null, openWorldHint: null)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    field1: Int @mcpToolAnnotations
                    field2: Int @mcpToolAnnotations(destructiveHint: null, idempotentHint: null, openWorldHint: null)
                }

                type Mutation {
                    field1: Int @mcpToolAnnotations
                    field2: Int @mcpToolAnnotations(destructiveHint: null, idempotentHint: null, openWorldHint: null)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
                mutation: Mutation
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type Mutation
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field1: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                field2: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            directive @mcpToolAnnotations(destructiveHint: Boolean idempotentHint: Boolean openWorldHint: Boolean) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Merge_McpToolAnnotationsDirectivesDestructiveHint_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    destructiveNullAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndNull: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveTrueAndTrue: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveFalseAndNull: Int @mcpToolAnnotations(destructiveHint: false)
                    destructiveFalseAndFalse: Int @mcpToolAnnotations(destructiveHint: false)
                }

                type Mutation {
                    destructiveNullAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndNull: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveTrueAndTrue: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveFalseAndNull: Int @mcpToolAnnotations(destructiveHint: false)
                    destructiveFalseAndFalse: Int @mcpToolAnnotations(destructiveHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    destructiveNullAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndTrue: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveFalseAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveFalseAndFalse: Int @mcpToolAnnotations(destructiveHint: false)
                }

                type Mutation {
                    destructiveNullAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveTrueAndTrue: Int @mcpToolAnnotations(destructiveHint: true)
                    destructiveFalseAndNull: Int @mcpToolAnnotations(destructiveHint: null)
                    destructiveFalseAndFalse: Int @mcpToolAnnotations(destructiveHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
                mutation: Mutation
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                destructiveFalseAndFalse: Int
                    @mcpToolAnnotations(destructiveHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveFalseAndNull: Int
                    @mcpToolAnnotations(destructiveHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveTrueAndNull: Int
                    @mcpToolAnnotations(destructiveHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveTrueAndTrue: Int
                    @mcpToolAnnotations(destructiveHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type Mutation
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                destructiveFalseAndFalse: Int
                    @mcpToolAnnotations(destructiveHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveFalseAndNull: Int
                    @mcpToolAnnotations(destructiveHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveTrueAndNull: Int
                    @mcpToolAnnotations(destructiveHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                destructiveTrueAndTrue: Int
                    @mcpToolAnnotations(destructiveHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            directive @mcpToolAnnotations(destructiveHint: Boolean idempotentHint: Boolean openWorldHint: Boolean) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Merge_McpToolAnnotationsDirectivesIdempotentHint_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    idempotentNullAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndNull: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentTrueAndTrue: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentFalseAndNull: Int @mcpToolAnnotations(idempotentHint: false)
                    idempotentFalseAndFalse: Int @mcpToolAnnotations(idempotentHint: false)
                }

                type Mutation {
                    idempotentNullAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndNull: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentTrueAndTrue: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentFalseAndNull: Int @mcpToolAnnotations(idempotentHint: false)
                    idempotentFalseAndFalse: Int @mcpToolAnnotations(idempotentHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    idempotentNullAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndTrue: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentFalseAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentFalseAndFalse: Int @mcpToolAnnotations(idempotentHint: false)
                }

                type Mutation {
                    idempotentNullAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentTrueAndTrue: Int @mcpToolAnnotations(idempotentHint: true)
                    idempotentFalseAndNull: Int @mcpToolAnnotations(idempotentHint: null)
                    idempotentFalseAndFalse: Int @mcpToolAnnotations(idempotentHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
                mutation: Mutation
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                idempotentFalseAndFalse: Int
                    @mcpToolAnnotations(idempotentHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentFalseAndNull: Int
                    @mcpToolAnnotations(idempotentHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentTrueAndNull: Int
                    @mcpToolAnnotations(idempotentHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentTrueAndTrue: Int
                    @mcpToolAnnotations(idempotentHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type Mutation
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                idempotentFalseAndFalse: Int
                    @mcpToolAnnotations(idempotentHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentFalseAndNull: Int
                    @mcpToolAnnotations(idempotentHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentTrueAndNull: Int
                    @mcpToolAnnotations(idempotentHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                idempotentTrueAndTrue: Int
                    @mcpToolAnnotations(idempotentHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            directive @mcpToolAnnotations(destructiveHint: Boolean idempotentHint: Boolean openWorldHint: Boolean) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Merge_McpToolAnnotationsDirectivesOpenWorldHint_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    openWorldNullAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndNull: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldTrueAndTrue: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldFalseAndNull: Int @mcpToolAnnotations(openWorldHint: false)
                    openWorldFalseAndFalse: Int @mcpToolAnnotations(openWorldHint: false)
                }

                type Mutation {
                    openWorldNullAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndNull: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldTrueAndTrue: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldFalseAndNull: Int @mcpToolAnnotations(openWorldHint: false)
                    openWorldFalseAndFalse: Int @mcpToolAnnotations(openWorldHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """,
                """
                # Schema B
                type Query {
                    openWorldNullAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndTrue: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldFalseAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldFalseAndFalse: Int @mcpToolAnnotations(openWorldHint: false)
                }

                type Mutation {
                    openWorldNullAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldTrueAndTrue: Int @mcpToolAnnotations(openWorldHint: true)
                    openWorldFalseAndNull: Int @mcpToolAnnotations(openWorldHint: null)
                    openWorldFalseAndFalse: Int @mcpToolAnnotations(openWorldHint: false)
                }

                directive @mcpToolAnnotations(
                    destructiveHint: Boolean
                    idempotentHint: Boolean
                    openWorldHint: Boolean
                ) on FIELD_DEFINITION
                """
            ],
            """
            schema {
                query: Query
                mutation: Mutation
            }

            type Query
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                openWorldFalseAndFalse: Int
                    @mcpToolAnnotations(openWorldHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldFalseAndNull: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldTrueAndNull: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldTrueAndTrue: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type Mutation
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                openWorldFalseAndFalse: Int
                    @mcpToolAnnotations(openWorldHint: false)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldFalseAndNull: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldNullAndNull: Int
                    @mcpToolAnnotations
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldTrueAndNull: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
                openWorldTrueAndTrue: Int
                    @mcpToolAnnotations(openWorldHint: true)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            directive @mcpToolAnnotations(destructiveHint: Boolean idempotentHint: Boolean openWorldHint: Boolean) on FIELD_DEFINITION
            """);
    }
}
