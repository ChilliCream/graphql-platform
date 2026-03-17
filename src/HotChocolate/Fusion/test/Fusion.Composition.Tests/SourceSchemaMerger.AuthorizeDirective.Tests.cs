using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerAuthorizeDirectiveTests : SourceSchemaMergerTestBase
{
    // Merge @authorize directives when the definitions match the canonical definition.
    [Fact]
    public void Merge_AuthorizeDirectives_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authorize(policy: "PolicyA1") {
                    field: Int @authorize(policy: "PolicyA2")
                }

                type FooObject @authorize(policy: "PolicyA3") {
                    field: Int @authorize(policy: "PolicyA4")
                }

                interface FooInterface {
                    field: Int @authorize(policy: "PolicyA5")
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """,
                $$"""
                # Schema B
                type Query @authorize(policy: "PolicyB1") {
                    field: Int @authorize(policy: "PolicyB2")
                }

                type FooObject @authorize(policy: "PolicyB3") {
                    field: Int @authorize(policy: "PolicyB4")
                }

                interface FooInterface {
                    field: Int @authorize(policy: "PolicyB5")
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "PolicyA1")
                @authorize(policy: "PolicyB1")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "PolicyA2")
                    @authorize(policy: "PolicyB2")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            type FooObject
                @authorize(policy: "PolicyA3")
                @authorize(policy: "PolicyB3")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "PolicyA4")
                    @authorize(policy: "PolicyB4")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }

            interface FooInterface
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "PolicyA5")
                    @authorize(policy: "PolicyB5")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Do not merge @authorize directives when the definitions do not match the canonical definition.
    [Fact]
    public void Merge_AuthorizeDirectivesNonMatching_MatchesSnapshot()
    {
        AssertMatches(
            [
                """
                # Schema A
                type Query @authorize(resource: "ResourceA1") {
                    field: Int
                }

                directive @authorize(resource: String) repeatable on OBJECT
                """,
                """
                # Schema B
                type Query {
                    field: Int @authorize
                }

                directive @authorize on FIELD_DEFINITION
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

    // Merge @authorize directives with the same policy name.
    [Fact]
    public void Merge_AuthorizeDirectivesSamePolicyName_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authorize(policy: "Policy1") {
                    field: Int @authorize(policy: "Policy2")
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """,
                $$"""
                # Schema B
                type Query @authorize(policy: "Policy1") {
                    field: Int @authorize(policy: "Policy2")
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "Policy1")
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "Policy2")
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same policy name but different apply policy.
    [Fact]
    public void Merge_AuthorizeDirectivesSamePolicyNameDifferentApplyPolicy_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authorize(policy: "Policy1", apply: BEFORE_RESOLVER) {
                    field: Int @authorize(policy: "Policy2", apply: AFTER_RESOLVER)
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """,
                $$"""
                # Schema B
                type Query @authorize(policy: "Policy1", apply: AFTER_RESOLVER) {
                    field: Int @authorize(policy: "Policy2", apply: BEFORE_RESOLVER)
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "Policy1", apply: BEFORE_RESOLVER)
                @authorize(policy: "Policy1", apply: AFTER_RESOLVER)
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "Policy2", apply: AFTER_RESOLVER)
                    @authorize(policy: "Policy2", apply: BEFORE_RESOLVER)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same roles (any order).
    [Fact]
    public void Merge_AuthorizeDirectivesSameRoles_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authorize(roles: ["Role1", "Role2"]) {
                    field: Int @authorize(roles: ["Role2", "Role1"])
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """,
                $$"""
                # Schema B
                type Query @authorize(roles: ["Role2", "Role1"]) {
                    field: Int @authorize(roles: ["Role1", "Role2"])
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(roles: ["Role1", "Role2"])
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(roles: ["Role1", "Role2"])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same roles (any order) but different apply policies.
    [Fact]
    public void Merge_AuthorizeDirectivesSameRolesDifferentApplyPolicy_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authorize(roles: ["Role1", "Role2"], apply: BEFORE_RESOLVER) {
                    field: Int @authorize(roles: ["Role2", "Role1"], apply: AFTER_RESOLVER)
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """,
                $$"""
                # Schema B
                type Query @authorize(roles: ["Role2", "Role1"], apply: AFTER_RESOLVER) {
                    field: Int @authorize(roles: ["Role1", "Role2"], apply: BEFORE_RESOLVER)
                }

                {{s_applyPolicyEnum}}
                {{s_authorizeDirective}}
                """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(roles: ["Role1", "Role2"], apply: BEFORE_RESOLVER)
                @authorize(roles: ["Role1", "Role2"], apply: AFTER_RESOLVER)
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(roles: ["Role1", "Role2"], apply: AFTER_RESOLVER)
                    @authorize(roles: ["Role1", "Role2"], apply: BEFORE_RESOLVER)
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same policy but different roles.
    [Fact]
    public void Merge_AuthorizeDirectivesSamePolicyDifferentRoles_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                  # Schema A
                  type Query @authorize(policy: "Policy1", roles: ["RoleA1", "RoleA2"]) {
                      field: Int @authorize(policy: "Policy2", roles: ["RoleA1", "RoleA2"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """,
                $$"""
                  # Schema B
                  type Query @authorize(policy: "Policy1", roles: ["RoleB1", "RoleB2"]) {
                      field: Int @authorize(policy: "Policy2", roles: ["RoleB1", "RoleB2"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "Policy1", roles: ["RoleA1", "RoleA2"])
                @authorize(policy: "Policy1", roles: ["RoleB1", "RoleB2"])
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "Policy2", roles: ["RoleA1", "RoleA2"])
                    @authorize(policy: "Policy2", roles: ["RoleB1", "RoleB2"])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same roles (any order) but different policy.
    [Fact]
    public void Merge_AuthorizeDirectivesSameRolesDifferentPolicy_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                  # Schema A
                  type Query @authorize(policy: "PolicyA1", roles: ["Role1", "Role2"]) {
                      field: Int @authorize(policy: "PolicyA2", roles: ["Role2", "Role1"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """,
                $$"""
                  # Schema B
                  type Query @authorize(policy: "PolicyB1", roles: ["Role2", "Role1"]) {
                      field: Int @authorize(policy: "PolicyB2", roles: ["Role1", "Role2"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "PolicyA1", roles: ["Role1", "Role2"])
                @authorize(policy: "PolicyB1", roles: ["Role1", "Role2"])
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "PolicyA2", roles: ["Role1", "Role2"])
                    @authorize(policy: "PolicyB2", roles: ["Role1", "Role2"])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    // Merge @authorize directives with the same policy and roles (any order).
    [Fact]
    public void Merge_AuthorizeDirectivesSamePolicyAndRoles_MatchesSnapshot()
    {
        AssertMatches(
            [
                $$"""
                  # Schema A
                  type Query @authorize(policy: "Policy1", roles: ["Role1", "Role2"]) {
                      field: Int @authorize(policy: "Policy2", roles: ["Role2", "Role1"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """,
                $$"""
                  # Schema B
                  type Query @authorize(policy: "Policy1", roles: ["Role2", "Role1"]) {
                      field: Int @authorize(policy: "Policy2", roles: ["Role1", "Role2"])
                  }

                  {{s_applyPolicyEnum}}
                  {{s_authorizeDirective}}
                  """
            ],
            """
            schema {
                query: Query
            }

            type Query
                @authorize(policy: "Policy1", roles: ["Role1", "Role2"])
                @fusion__type(schema: A)
                @fusion__type(schema: B) {
                field: Int
                    @authorize(policy: "Policy2", roles: ["Role1", "Role2"])
                    @fusion__field(schema: A)
                    @fusion__field(schema: B)
            }
            """,
            modifySchema: s_removeSerializeAsDirective);
    }

    private static readonly ApplyPolicyMutableEnumTypeDefinition s_applyPolicyEnum = new();

    private static readonly AuthorizeMutableDirectiveDefinition s_authorizeDirective
        = new(BuiltIns.String.Create(), s_applyPolicyEnum);

    private static readonly Action<MutableSchemaDefinition> s_removeSerializeAsDirective
        = schema =>
        {
            schema.DirectiveDefinitions.Remove(WellKnownDirectiveNames.Authorize);
            schema.Types.Remove(WellKnownTypeNames.ApplyPolicy);
        };
}
