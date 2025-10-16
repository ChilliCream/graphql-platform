namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RootSubscriptionUsedRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RootSubscriptionUsedRule();

    // Valid example.
    [Fact]
    public void Validate_RootSubscriptionUsed_Succeeds()
    {
        AssertValid(
        [
            """
            schema {
                subscription: Subscription
            }

            type Subscription {
                productCreated: Product
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // The following example violates the rule because "RootSubscription" is used as the root
    // subscription type, but a type named "Subscription" is also defined.
    [Fact]
    public void Validate_RootSubscriptionUsedDifferentName_Fails()
    {
        AssertInvalid(
            [
                """
                schema {
                    subscription: RootSubscription
                }

                type RootSubscription {
                    productCreated: Product
                }

                type Subscription {
                    deprecatedField: String
                }
                """
            ],
            [
                "The root subscription type in schema 'A' must be named 'Subscription'."
            ]);
    }

    // A type named "Subscription" is not the root subscription type.
    [Fact]
    public void Validate_RootSubscriptionUsedNotRootType_Fails()
    {
        AssertInvalid(
            [
                "scalar Subscription"
            ],
            [
                "The root subscription type in schema 'A' must be named 'Subscription'."
            ]);
    }
}
