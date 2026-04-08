namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class InvalidShareableUsageRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InvalidShareableUsageRule();

    // In this example, the field "orderStatus" on the "Order" object type is marked with
    // @shareable, which is allowed. It signals that this field can be served from multiple schemas
    // without creating a conflict.
    [Fact]
    public void Validate_ValidShareableUsage_Succeeds()
    {
        AssertValid(
        [
            """
            type Order {
                id: ID!
                orderStatus: String @shareable
                total: Float
            }
            """
        ]);
    }

    // In this example, the "InventoryItem" interface has a field "sku" marked with @shareable,
    // which is invalid usage. Marking an interface field as shareable leads to an
    // INVALID_SHAREABLE_USAGE error.
    [Fact]
    public void Validate_InvalidShareableUsageInterfaceField_Fails()
    {
        AssertInvalid(
            [
                """
                interface InventoryItem {
                    sku: ID! @shareable
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'InventoryItem.sku' in schema 'A' must not be marked as shareable.",
                    "code": "INVALID_SHAREABLE_USAGE",
                    "severity": "Error",
                    "coordinate": "InventoryItem.sku",
                    "member": "sku",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // By definition, root subscription fields cannot be shared across multiple schemas. In this
    // example, both schemas define a subscription field "newOrderPlaced".
    [Fact]
    public void Validate_InvalidShareableUsageSubscriptionField_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Subscription {
                    newOrderPlaced: Order @shareable
                }

                type Order {
                    id: ID!
                    items: [String]
                }
                """,
                """
                # Schema B
                type Subscription {
                    newOrderPlaced: Order @shareable
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'Subscription.newOrderPlaced' in schema 'A' must not be marked as shareable.",
                    "code": "INVALID_SHAREABLE_USAGE",
                    "severity": "Error",
                    "coordinate": "Subscription.newOrderPlaced",
                    "member": "newOrderPlaced",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The field 'Subscription.newOrderPlaced' in schema 'B' must not be marked as shareable.",
                    "code": "INVALID_SHAREABLE_USAGE",
                    "severity": "Error",
                    "coordinate": "Subscription.newOrderPlaced",
                    "member": "newOrderPlaced",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
