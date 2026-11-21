namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireContextOnInterfaceRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireContextOnInterfaceRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheContextRequirementIsOnAnObjectFieldWithoutAnInterface()
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
    public void Validate_Should_Succeed_When_TheRequirementOnAnInterfaceFieldHasNoContextPath()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            interface Priced {
                price(name: String! @require(field: "name")): Float
            }

            type Product implements Priced {
                name: String!
                price(name: String!): Float
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_TheImplementedInterfaceDoesNotDeclareTheField()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            interface Named {
                name: String!
            }

            type Tenant @context(name: "tenantContext") {
                id: ID!
            }

            type Product implements Named {
                name: String!
                price(tenant: ID! @require(field: "$tenantContext.id")): Float
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextRequirementIsOnAnInterfaceField()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                interface Priced {
                    price(tenant: ID! @require(field: "$tenantContext.id")): Float
                }

                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Priced.price(tenant:)' in schema 'A' contains a context path, which is not allowed on an interface field or on a field that implements an interface field.",
                    "code": "REQUIRE_CONTEXT_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Priced.price(tenant:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextRequirementIsOnAFieldThatImplementsAnInterfaceField()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                interface Priced {
                    price: Float
                }

                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }

                type Product implements Priced {
                    price(tenant: ID! @require(field: "$tenantContext.id")): Float
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.price(tenant:)' in schema 'A' contains a context path, which is not allowed on an interface field or on a field that implements an interface field.",
                    "code": "REQUIRE_CONTEXT_ON_INTERFACE",
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
