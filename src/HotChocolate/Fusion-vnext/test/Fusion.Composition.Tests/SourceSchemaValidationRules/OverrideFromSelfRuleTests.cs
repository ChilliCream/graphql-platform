namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class OverrideFromSelfRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new OverrideFromSelfRule();

    // In the following example, Schema B overrides the field "amount" from Schema A. The two schema
    // names are different, so no error is raised.
    [Fact]
    public void Validate_NoOverrideFromSelf_Succeeds()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Bill {
                id: ID!
                amount: Int
            }
            """,
            """
            # Source Schema B
            type Bill {
                id: ID!
                amount: Int @override(from: "A")
            }
            """
        ]);
    }

    // In the following example, the local schema is also "A", and the "from" argument is "A".
    // Overriding a field from the same schema is not allowed, causing an OVERRIDE_FROM_SELF error.
    [Fact]
    public void Validate_OverrideFromSelf_Fails()
    {
        AssertInvalid(
            [
                """
                # Source Schema A (named "A")
                type Bill {
                    id: ID!
                    amount: Int @override(from: "A")
                }
                """
            ],
            [
                "The @override directive on field 'Bill.amount' in schema 'A' must not reference "
                + "the same schema."
            ]);
    }
}
