namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class InterfaceFieldNoImplementationRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InterfaceFieldNoImplementationRule();

    // In this example, the "User" interface has three fields: "id", "name", and "email". Both the
    // "RegisteredUser" and "GuestUser" types implement all three fields, satisfying the interface
    // contract.
    [Fact]
    public void Validate_InterfaceFieldWithImplementation_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface User {
                id: ID!
                name: String!
                email: String
            }

            type RegisteredUser implements User {
                id: ID!
                name: String!
                email: String
                lastLogin: DateTime
            }
            """,
            """
            # Schema B
            interface User {
                id: ID!
                name: String!
                email: String
            }

            type GuestUser implements User {
                id: ID!
                name: String!
                email: String
                temporaryCartId: String
            }
            """
        ]);
    }

    // Here, the "email" field on the "User" interface is marked as @inaccessible, so it does not
    // need to be implemented by the "GuestUser" type.
    [Fact]
    public void Validate_InterfaceFieldWithImplementationInaccessibleInterfaceField_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface User {
                id: ID!
                name: String!
                email: String @inaccessible
            }

            type RegisteredUser implements User {
                id: ID!
                name: String!
                email: String
                lastLogin: DateTime
            }
            """,
            """
            # Schema B
            interface User {
                id: ID!
                name: String!
            }

            type GuestUser implements User {
                id: ID!
                name: String!
                temporaryCartId: String
            }
            """
        ]);
    }

    // In this example, the "User" interface is defined with three fields, but the "GuestUser" type
    // omits one of them ("email"), causing an INTERFACE_FIELD_NO_IMPLEMENTATION error.
    [Fact]
    public void Validate_InterfaceFieldNoImplementation_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface User {
                    id: ID!
                    name: String!
                    email: String
                }

                type RegisteredUser implements User {
                    id: ID!
                    name: String!
                    email: String
                    lastLogin: DateTime
                }
                """,
                """
                # Schema B
                interface User {
                    id: ID!
                    name: String!
                }

                type GuestUser implements User {
                    id: ID!
                    name: String!
                    temporaryCartId: String
                }
                """
            ],
            [
                """
                {
                    "message": "The merged object type 'GuestUser' must implement the field 'email' on interface 'User'.",
                    "code": "INTERFACE_FIELD_NO_IMPLEMENTATION",
                    "severity": "Error",
                    "coordinate": "GuestUser",
                    "member": "GuestUser",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
