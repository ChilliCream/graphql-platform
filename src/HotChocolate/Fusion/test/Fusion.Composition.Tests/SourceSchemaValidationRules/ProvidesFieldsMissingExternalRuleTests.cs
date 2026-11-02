namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesFieldsMissingExternalRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesFieldsMissingExternalRule();

    // Here, the "Order" type from this schema is providing fields on "User" through @provides. The
    // "name" field of "User" is not defined in this schema; it is declared with @external
    // indicating that the "name" field comes from elsewhere. Thus, referencing "name" under
    // @provides(fields: "name") is valid.
    [Fact]
    public void Validate_ProvidesFieldsNotMissingExternal_Succeeds()
    {
        AssertValid(
        [
            """
            type Order {
                id: ID!
                customer: User @provides(fields: "name")
            }

            type User @key(fields: "id") {
                id: ID!
                name: String @external
            }
            """
        ]);
    }

    // In this example, "User.address" is not marked as @external in the same schema that applies
    // @provides. This means the schema already provides the "address" field in all possible paths,
    // so using @provides(fields: "address") is invalid.
    [Fact]
    public void Validate_ProvidesFieldsMissingExternal_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    id: ID!
                    address: String
                }

                type Order {
                    id: ID!
                    buyer: User @provides(fields: "address")
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'Order.buyer' in schema 'A' references field 'User.address', which must be marked as external.",
                    "code": "PROVIDES_FIELDS_MISSING_EXTERNAL",
                    "severity": "Error",
                    "coordinate": "Order.buyer",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested field.
    [Fact]
    public void Validate_ProvidesFieldsMissingExternalNestedField_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    id: ID!
                    info: UserInfo
                }

                type UserInfo {
                    address: String
                }

                type Order {
                    id: ID!
                    buyer: User @provides(fields: "info { address }")
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'Order.buyer' in schema 'A' references field 'User.info', which must be marked as external.",
                    "code": "PROVIDES_FIELDS_MISSING_EXTERNAL",
                    "severity": "Error",
                    "coordinate": "Order.buyer",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The @provides directive on field 'Order.buyer' in schema 'A' references field 'UserInfo.address', which must be marked as external.",
                    "code": "PROVIDES_FIELDS_MISSING_EXTERNAL",
                    "severity": "Error",
                    "coordinate": "Order.buyer",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
