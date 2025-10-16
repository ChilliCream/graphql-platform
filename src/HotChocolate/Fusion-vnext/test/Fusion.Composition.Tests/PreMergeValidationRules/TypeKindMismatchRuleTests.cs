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
                "The type 'User' has a different kind in schema 'A' (Object) than it does in "
                + "schema 'B' (Interface)."
            ]);
    }
}
