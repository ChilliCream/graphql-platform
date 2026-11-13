namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class RequireContextNotSetRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireContextNotSetRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheContextIsSetByAType()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Query {
                tenant: Tenant
                product: Product
            }

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
    public void Validate_Should_Succeed_When_TheRequireMapHasNoContextPath()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Product {
                id: ID!
                price(name: String! @require(field: "name")): Float
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextIsNotSetByAnyType()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Product {
                    price(tenant: ID! @require(field: "$tenantContext.id")): Float
                }

                type Tenant @context(name: "otherContext") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.price(tenant:)' in schema 'A' references the context 'tenantContext', which is not set by any type in the schema.",
                    "code": "REQUIRE_CONTEXT_NOT_SET",
                    "severity": "Error",
                    "coordinate": "Product.price(tenant:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheContextIsSetOnlyInAnotherSchema()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Product {
                    price(tenant: ID! @require(field: "$tenantContext.id")): Float
                }
                """,
                """
                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.price(tenant:)' in schema 'A' references the context 'tenantContext', which is not set by any type in the schema.",
                    "code": "REQUIRE_CONTEXT_NOT_SET",
                    "severity": "Error",
                    "coordinate": "Product.price(tenant:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_ReportEachMissingContext_When_TheMapReferencesSeveralContexts()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Product {
                    price(
                        input: PriceInput!
                            @require(field: "{ tenant: $tenantContext.id, user: $userContext.id, region: $regionContext.id }")): Float
                }

                input PriceInput {
                    tenant: ID!
                    user: ID!
                    region: ID!
                }

                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @require directive on argument 'Product.price(input:)' in schema 'A' references the context 'userContext', which is not set by any type in the schema.",
                    "code": "REQUIRE_CONTEXT_NOT_SET",
                    "severity": "Error",
                    "coordinate": "Product.price(input:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The @require directive on argument 'Product.price(input:)' in schema 'A' references the context 'regionContext', which is not set by any type in the schema.",
                    "code": "REQUIRE_CONTEXT_NOT_SET",
                    "severity": "Error",
                    "coordinate": "Product.price(input:)",
                    "member": "require",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
