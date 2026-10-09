namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class TypeKindMismatchRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new TypeKindMismatchRule();

    // All schemas agree that "User" is an object type.
    [Fact]
    public void Validate_TypeKindMatch_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User {
                id: ID!
                name: String
            }
            """,
            """
            # Schema B
            type User {
                id: ID!
                email: String
            }
            """
        ]);
    }

    // In the following example, "User" is defined as an object type in one of the schemas and as an
    // interface in another. This violates the rule and results in a TYPE_KIND_MISMATCH error.
    [Fact]
    public void Validate_TypeKindMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type User {
                    id: ID!
                    name: String
                }
                """,
                """
                # Schema B
                interface User {
                    id: ID!
                    friends: [User!]!
                }
                """
            ],
            [
                """
                {
                    "message": "The type 'User' has a different kind in schema 'A' (Object) than it does in schema 'B' (Interface).",
                    "code": "TYPE_KIND_MISMATCH",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "User",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // "Media" is an interface in schema A and an @interfaceObject stand-in (object type) in schema B.
    // The stand-in is excluded from the kind check, so this is not a mismatch.
    [Fact]
    public void Validate_StandInExcludedFromKindCheck_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Media {
                id: ID!
                title: String!
            }
            """,
            """
            # Schema B
            type Media @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                rating: Int!
            }
            """
        ]);
    }

    // "Media" is an interface in schema A and a plain object type (no @interfaceObject) in schema B.
    // The stand-in exception does not apply, so this is a genuine TYPE_KIND_MISMATCH.
    [Fact]
    public void Validate_PlainObjectVsInterface_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Media {
                    id: ID!
                    title: String!
                }
                """,
                """
                # Schema B
                type Media {
                    id: ID!
                    reviewCount: Int!
                }
                """
            ],
            [
                """
                {
                    "message": "The type 'Media' has a different kind in schema 'A' (Interface) than it does in schema 'B' (Object).",
                    "code": "TYPE_KIND_MISMATCH",
                    "severity": "Error",
                    "coordinate": "Media",
                    "member": "Media",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
