namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalOverrideCollisionRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalOverrideCollisionRule();

    // In this scenario, "User.fullName" is defined in Schema A, but overridden in Schema B. Since
    // @override is not combined with @external on the same field, no collision occurs.
    [Fact]
    public void Validate_NoExternalOverrideCollision_Succeeds()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type User {
                id: ID!
                fullName: String
            }
            """,
            """
            # Source Schema B
            type User {
                id: ID!
                fullName: String @override(from: "A")
            }
            """
        ]);
    }

    // Here, "amount" is marked with both @override and @external. This violates the rule because
    // the field is simultaneously labeled as "override from another schema" and "external" in the
    // local schema, producing an EXTERNAL_OVERRIDE_COLLISION error.
    [Fact]
    public void Validate_ExternalOverrideCollision_Fails()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                type Payment {
                    id: ID!
                    amount: Int
                }
                """,
                """
                # Source Schema B
                type Payment {
                    id: ID!
                    amount: Int @override(from: "A") @external
                }
                """
            ],
            [
                "The external field 'Payment.amount' in schema 'B' must not be annotated with the "
                + "@override directive."
            ]);
    }
}
