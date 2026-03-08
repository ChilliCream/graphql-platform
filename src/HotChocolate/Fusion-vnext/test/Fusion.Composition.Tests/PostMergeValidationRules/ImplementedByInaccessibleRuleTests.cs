namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ImplementedByInaccessibleRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ImplementedByInaccessibleRule();

    // In the following example, "User.id" is accessible and implements "Node.id" which is also
    // accessible, no error occurs.
    [Fact]
    public void Validate_NotImplementedByInaccessible_Succeeds()
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
            """
        ]);
    }

    // Since "Auditable" and its field "lastAudit" are @inaccessible, the "Order.lastAudit" field is
    // allowed to be @inaccessible because it does not implement any visible interface field in the
    // composed schema.
    [Fact]
    public void Validate_NotImplementedByInaccessibleObjectFieldInaccessible_Succeeds()
    {
        AssertValid(
        [
            """
            interface Auditable @inaccessible {
                lastAudit: DateTime!
            }

            type Order implements Auditable {
                lastAudit: DateTime! @inaccessible
                orderNumber: String
            }
            """
        ]);
    }

    // Accessible interface field "User.id" implementing accessible field "Node.id" in another
    // interface.
    [Fact]
    public void Validate_NotImplementedByInaccessibleFieldAccessible_Succeeds()
    {
        AssertValid(
        [
            """
            interface Node {
                id: ID!
            }

            interface User implements Node {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // Inaccessible interface field "Order.lastAudit" implementing inaccessible field
    // "Auditable.lastAudit" in another interface.
    [Fact]
    public void Validate_NotImplementedByInaccessibleInterfaceFieldInaccessible_Succeeds()
    {
        AssertValid(
        [
            """
            interface Auditable @inaccessible {
                lastAudit: DateTime!
            }

            interface Order implements Auditable {
                lastAudit: DateTime! @inaccessible
                orderNumber: String
            }
            """
        ]);
    }

    // In this example, "Node.id" is visible in the public schema (no @inaccessible), but "User.id"
    // is marked @inaccessible. This violates the interface contract because "User" claims to
    // implement "Node", yet does not expose the "id" field to the public schema.
    [Fact]
    public void Validate_ImplementedByInaccessibleObjectFieldInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                interface Node {
                    id: ID!
                }

                type User implements Node {
                    id: ID! @inaccessible
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.id' implementing interface field 'Node.id' is inaccessible in the composed schema.",
                    "code": "IMPLEMENTED_BY_INACCESSIBLE",
                    "severity": "Error",
                    "coordinate": "User.id",
                    "member": "id",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }

    // Same as above, for an interface type.
    [Fact]
    public void Validate_ImplementedByInaccessibleInterfaceFieldInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                interface Node {
                    id: ID!
                }

                interface User implements Node {
                    id: ID! @inaccessible
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'User.id' implementing interface field 'Node.id' is inaccessible in the composed schema.",
                    "code": "IMPLEMENTED_BY_INACCESSIBLE",
                    "severity": "Error",
                    "coordinate": "User.id",
                    "member": "id",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
