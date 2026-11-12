namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireContextDefaultValueRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireContextDefaultValueRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheContextArgumentHasNoDefaultValue()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Tenant @context(name: "tenantContext") {
                id: ID!
            }

            type Product {
                price(tenant: ID! @require(field: "$tenantContext.id")): Float
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_TheArgumentWithoutContextPathHasADefaultValue()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Product {
                name: String
                price(label: String = "x" @require(field: "name")): Float
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextArgumentHasADefaultValue()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }

                type Product {
                    price(tenant: ID = "1" @require(field: "$tenantContext.id")): Float
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'Product.price(tenant:)' in schema 'A' reads from a context and must not have a default value.",
                    "code": "REQUIRE_CONTEXT_DEFAULT_VALUE",
                    "severity": "Error",
                    "coordinate": "Product.price(tenant:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
