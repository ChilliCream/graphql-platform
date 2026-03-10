namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalOnInterfaceRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalOnInterfaceRule();

    // Here, the interface "Node" merely describes the field "id". Object types "User" and "Product"
    // implement and resolve "id". No @external usage occurs on the interface itself, so no error is
    // triggered.
    [Fact]
    public void Validate_ExternalNotOnInterface_Succeeds()
    {
        AssertValid(
        [
            """
            interface Node {
                id: ID!
            }

            type User implements Node {
                id: ID!
                name: String
            }

            type Product implements Node {
                id: ID!
                price: Int
            }
            """
        ]);
    }

    // Since "id" is declared on an interface and marked with @external, the composition fails with
    // EXTERNAL_ON_INTERFACE. An interface does not own the concrete field resolution, so it is
    // invalid to mark any of its fields as external.
    [Fact]
    public void Validate_ExternalOnInterface_Fails()
    {
        AssertInvalid(
            [
                """
                interface Node {
                    id: ID! @external
                }
                """
            ],
            [
                """
                {
                    "message": "The interface field 'Node.id' in schema 'A' must not be marked as external.",
                    "code": "EXTERNAL_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Node.id",
                    "member": "id",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
