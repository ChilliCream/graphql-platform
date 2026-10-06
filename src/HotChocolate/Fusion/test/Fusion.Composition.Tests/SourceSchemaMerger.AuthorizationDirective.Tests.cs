namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerAuthorizationDirectiveTests : SourceSchemaMergerTestBase
{
    private const string AuthorizationDirectives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Merge_Should_EmitFusionAuthorization_When_MembersCarrySourceDirectives()
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

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__authorization(authenticated: true) @fusion__type(schema: A) {
              enumField: FooEnum @fusion__field(schema: A)
              field: Int
                @fusion__authorization(scopes: [["read"]])
                @fusion__field(schema: A)
              interfaceField: FooInterface @fusion__field(schema: A)
              scalarField: FooScalar @fusion__field(schema: A)
            }

            interface FooInterface
              @fusion__authorization(policies: [["Admin"]])
              @fusion__type(schema: A) {
              id: Int @fusion__field(schema: A)
            }

            enum FooEnum
              @fusion__authorization(authenticated: true)
              @fusion__type(schema: A) {
              ONE @fusion__enumValue(schema: A)
            }

            scalar FooScalar
              @fusion__authorization(scopes: [["scalar"]])
              @fusion__type(schema: A)
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

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int
                    other: Int
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__authorization(authenticated: true)
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

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @requiresScopes(scopes: [["c"], ["d"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__authorization(
                  scopes: [["a", "c"], ["a", "d"], ["b", "c"], ["b", "d"]]
                )
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

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @policy(policies: [["x"], ["a", "b"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__authorization(policies: [["a", "b"], ["a", "x"], ["b", "m", "x"]])
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

                {{AuthorizationDirectives}}
                """,
                $$"""
                # Schema B
                type Query {
                    field: Int @policy(policies: [["Admin"]])
                }

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              field: Int
                @fusion__authorization(
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

                {{AuthorizationDirectives}}
                """
            ],
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              policed: Int
                @fusion__authorization(policies: [["p"]])
                @fusion__field(schema: A)
              scoped: Int
                @fusion__authorization(scopes: [["read"]])
                @fusion__field(schema: A)
            }
            """);
    }
}
