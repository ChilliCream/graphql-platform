using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerPolicyDirectiveTests : SourceSchemaMergerTestBase
{
    // Composition-level guard: an @interfaceObject stand-in is a MutableObjectTypeDefinition, not an
    // interface, so it is not caught by the merge-time interface handling. Source validation is the
    // only place that can reject it before its @policy application would otherwise reach the merged
    // interface field unenforced. This pin guards against that ingress reopening as a fail-open
    // regression.
    [Fact]
    public void Compose_Should_Fail_When_PolicyIsOnInterfaceObjectStandInField()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    interface Node @key(fields: "id") {
                        id: ID!
                        title: String!
                    }

                    type Query {
                        node: Node
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    $$"""
                    type Node @interfaceObject @key(fields: "id") {
                        id: ID!
                        title: String! @policy(names: "CanReadTitle")
                    }

                    {{s_policyDenialBehaviorEnum}}
                    {{s_policyDirective}}
                    """)
            ],
            new SchemaComposerOptions
            {
                Merger = { AddFusionDefinitions = false, RemoveUnreferencedDefinitions = false }
            },
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(log, e => e.Code == LogEntryCodes.PolicyOnInterface);
    }

    [Fact]
    public void Merge_Should_StampFusionPolicy_When_FieldHasPolicy()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    product: Product @policy(names: "CanReadProduct", onDenied: ERROR)
                }

                type Product {
                    id: ID!
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadProduct", onDenied: ERROR)
            }

            type Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    // Pins compatibility against Core's actual printed @policy shape (nullable onDenied, no
    // default value) rather than composition's own mirror of it, so a drift between the two
    // canonical definitions is caught here instead of silently dropping @policy applications
    // from real HC source schemas.
    [Fact]
    public void Merge_Should_StampFusionPolicy_When_SourceUsesCoresCanonicalDirectiveShape()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query {
                    product: Product @policy(names: "CanReadProduct", onDenied: ERROR)
                }

                type Product {
                    id: ID!
                }

                "Defines the consequence that applies when a policy expression denies access."
                enum PolicyDenialBehavior {
                  "The guarded value is set to null without an error."
                  NULL
                  "The guarded value is set to null and an authorization error is added."
                  ERROR
                  "The request is terminated."
                  ABORT
                }

                directive @policy(
                  "The policy expression in disjunctive normal form. Names within an inner list combine with AND, the outer list combines with OR."
                  names: [[String!]!]!
                  "The consequence that applies when the policy expression denies access."
                  onDenied: PolicyDenialBehavior
                ) repeatable on OBJECT | FIELD_DEFINITION
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              product: Product
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadProduct", onDenied: ERROR)
            }

            type Product @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_DedupToMaxOnDenied_When_SameApplicationFromTwoSources()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: "CanRead", onDenied: ERROR)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: String @policy(names: "CanRead", onDenied: ABORT)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__policy(names: "CanRead", onDenied: ABORT)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_MergeToSingleApplication_When_GroupOrderDiffersAcrossSources()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: [["CanRead", "CanAudit"]], onDenied: ERROR)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: String @policy(names: [["CanAudit", "CanRead"]], onDenied: ABORT)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__policy(names: [["CanAudit", "CanRead"]], onDenied: ABORT)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_KeepSeparateApplications_When_ExpressionsDiffer()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String
                        @policy(names: [["CanRead"], ["CanAudit"]])
                        @policy(names: "CanAdmin", onDenied: ERROR)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: [["CanAudit"], ["CanRead"]], onDenied: NULL)
                @fusion__policy(names: "CanAdmin", onDenied: ERROR)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_WriteBareString_When_ExpressionIsSingleNameGroup()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: [["CanRead"]], onDenied: ABORT)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: ABORT)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_StampSchemaWideDefault_When_OnDeniedIsAbsentFromEverySource()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: "CanRead")
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: NULL)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_UseConfiguredDefault_When_OnDeniedIsAbsentFromEverySource()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: "CanRead")
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: ABORT)
            }
            """,
            configure: o => o.PolicyOnDeniedDefault = PolicyDenialBehavior.Abort,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_KeepExplicitNull_When_SchemaWideDefaultIsError()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: "CanRead", onDenied: NULL)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: String
                @fusion__field(schema: A)
                @fusion__policy(names: "CanRead", onDenied: NULL)
            }
            """,
            configure: o => o.PolicyOnDeniedDefault = PolicyDenialBehavior.Error,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_IgnoreAbsentContribution_When_AnotherSourceIsExplicit()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: String @policy(names: "CanRead", onDenied: NULL)
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: String @policy(names: "CanRead")
                }

                {{s_policyDenialBehaviorEnum}}
                {{s_policyDirective}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: String
                @fusion__field(schema: A)
                @fusion__field(schema: B)
                @fusion__policy(names: "CanRead", onDenied: NULL)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    private static readonly PolicyDenialBehaviorMutableEnumTypeDefinition s_policyDenialBehaviorEnum = new();

    private static readonly PolicyMutableDirectiveDefinition s_policyDirective
        = new(BuiltIns.String.Create(), s_policyDenialBehaviorEnum);

    private static readonly Action<MutableSchemaDefinition> s_removePolicyDirective
        = schema =>
        {
            schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.Policy);
            schema.Types.Remove(WellKnownTypeNames.PolicyDenialBehavior);
        };
}
