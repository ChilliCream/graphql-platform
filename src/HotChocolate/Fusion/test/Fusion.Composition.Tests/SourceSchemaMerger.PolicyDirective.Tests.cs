using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerPolicyDirectiveTests : SourceSchemaMergerTestBase
{
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

    [Fact]
    public void Merge_Should_CopyToImplementor_When_InterfaceTypeHasPolicy()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    product: Product
                }

                interface Node @policy(names: "CanReadNode", onDenied: ERROR) {
                    id: ID!
                }

                type Product implements Node {
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
              product: Product @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node")
              @fusion__policy(names: "CanReadNode", onDenied: ERROR) {
              id: ID! @fusion__field(schema: A)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_CopyToImplementorField_When_InterfaceFieldHasPolicy()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    product: Product
                }

                interface Node {
                    id: ID! @policy(names: "CanReadNodeId", onDenied: ERROR)
                }

                type Product implements Node {
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
              product: Product @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID!
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadNodeId", onDenied: ERROR)
            }

            interface Node @fusion__type(schema: A) {
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
                @fusion__policy(names: [["CanAudit"], ["CanRead"]])
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
                    field: String @policy(names: [["CanRead"]])
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
              field: String @fusion__field(schema: A) @fusion__policy(names: "CanRead")
            }
            """,
            modifySchema: s_removePolicyDirective);
    }

    [Fact]
    public void Merge_Should_RemoveUserPolicyFromInterfaces_When_Merged()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    product: Product
                }

                interface Node @policy(names: "CanReadNode") {
                    id: ID! @policy(names: "CanReadNodeId", onDenied: ERROR)
                }

                type Product implements Node {
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
              product: Product @fusion__field(schema: A)
            }

            type Product implements Node
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node")
              @fusion__policy(names: "CanReadNode") {
              id: ID!
                @fusion__field(schema: A)
                @fusion__policy(names: "CanReadNodeId", onDenied: ERROR)
            }

            interface Node @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
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
