namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerPolicyDirectiveTests : SourceSchemaMergerTestBase
{
    private const string PolicyDirectives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Merge_Should_EmitFusionPolicy_When_MembersCarrySourceDirectives()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query @authenticated {
                    field: Int @requiresScopes(scopes: [["read"]])
                    interfaceField: FooInterface
                    enumField: FooEnum
                    scalarField: FooScalar
                }

                interface FooInterface @policy(policies: [["Admin"]]) {
                    id: Int
                }

                enum FooEnum @authenticated {
                    ONE
                }

                scalar FooScalar @requiresScopes(scopes: [["scalar"]])

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__policy(authenticated: true) @fusion__type(schema: A) {
              enumField: FooEnum
                @fusion__policy(authenticated: true)
                @fusion__field(schema: A)
              field: Int
                @fusion__policy(authenticated: true, scopes: [["read"]])
                @fusion__field(schema: A)
              interfaceField: FooInterface
                @fusion__policy(authenticated: true, policies: [["Admin"]])
                @fusion__field(schema: A)
              scalarField: FooScalar
                @fusion__policy(authenticated: true, scopes: [["scalar"]])
                @fusion__field(schema: A)
            }

            interface FooInterface
              @fusion__policy(policies: [["Admin"]])
              @fusion__type(schema: A) {
              id: Int @fusion__policy(policies: [["Admin"]]) @fusion__field(schema: A)
            }

            enum FooEnum @fusion__policy(authenticated: true) @fusion__type(schema: A) {
              ONE @fusion__enumValue(schema: A)
            }

            scalar FooScalar @fusion__policy(scopes: [["scalar"]]) @fusion__type(schema: A)
            """);
    }

    [Fact]
    public void Merge_Should_MarkAuthenticated_When_AnySourceMarksTheMember()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @authenticated
                    other: Int
                }

                {{PolicyDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int
                    other: Int
                }

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__policy(authenticated: true)
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              other: Int @fusion__field(schema: A) @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_BuildCartesianProductOfScopes_When_MultipleSourcesRequireScopes()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @requiresScopes(scopes: [["a"], ["b"]])
                }

                {{PolicyDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @requiresScopes(scopes: [["c"], ["d"]])
                }

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__policy(scopes: [["a", "c"], ["a", "d"], ["b", "c"], ["b", "d"]])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_WriteCanonicalPolicies_When_GroupsAreUnsortedDuplicatedOrSupersets()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @policy(policies: [["z", "a"], ["a"], ["m", "b"], ["b", "m"]])
                }

                {{PolicyDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @policy(policies: [["x"], ["a", "b"]])
                }

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__policy(policies: [["a", "b"], ["a", "x"], ["b", "m", "x"]])
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_CombineAuthenticatedScopesAndPolicies_When_SourcesDeclareDifferentParts()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    field: Int @authenticated @requiresScopes(scopes: [["read"]])
                }

                {{PolicyDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @policy(policies: [["Admin"]])
                }

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__policy(
                  authenticated: true
                  scopes: [["read"]]
                  policies: [["Admin"]]
                )
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """);
    }

    [Fact]
    public void Merge_Should_DropAuthorize_When_SourceSchemaUsesAuthorize()
    {
        // arrange & act & assert
        AssertMatches(
            [
                """
                # Schema A
                type Query @authorize(policy: "PolicyA1") {
                    field: Int @authorize(policy: "PolicyA2")
                }

                directive @authorize(policy: String) repeatable on FIELD_DEFINITION | OBJECT
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              field: Int @fusion__field(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_Should_EmitCanonicalGroups_When_ArgumentIsASingleString()
    {
        // arrange & act & assert
        AssertMatches(
            [
                $$"""
                # Schema A
                type Query {
                    scoped: Int @requiresScopes(scopes: "read")
                    policed: Int @policy(policies: "p")
                }

                {{PolicyDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              policed: Int @fusion__policy(policies: [["p"]]) @fusion__field(schema: A)
              scoped: Int @fusion__policy(scopes: [["read"]]) @fusion__field(schema: A)
            }
            """);
    }
}
