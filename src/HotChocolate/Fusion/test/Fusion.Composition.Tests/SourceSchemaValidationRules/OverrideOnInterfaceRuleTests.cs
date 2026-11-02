namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class OverrideOnInterfaceRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new OverrideOnInterfaceRule();

    // In this example, @override is used on a field of an object type, ensuring that the field
    // definition is concrete and can be reassigned to another schema.
    [Fact]
    public void Validate_NoOverrideOnInterface_Succeeds()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Order {
                id: ID!
                amount: Int
            }
            """,
            """
            # Source Schema B
            type Order {
                id: ID!
                amount: Int @override(from: "A")
            }
            """
        ]);
    }

    // In the following example, "Bill.amount" is declared on an interface type and annotated with
    // @override. This violates the rule because the interface field itself is not eligible for
    // ownership transfer. The composition fails with an OVERRIDE_ON_INTERFACE error.
    [Fact]
    public void Validate_OverrideOnInterface_Fails()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                interface Bill {
                    id: ID!
                    amount: Int @override(from: "B")
                }
                """
            ],
            [
                """
                {
                    "message": "The interface field 'Bill.amount' in schema 'A' must not be annotated with the @override directive.",
                    "code": "OVERRIDE_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Bill.amount",
                    "member": "amount",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
