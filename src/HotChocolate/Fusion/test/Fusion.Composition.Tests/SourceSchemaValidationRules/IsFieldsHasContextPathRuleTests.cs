namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class IsFieldsHasContextPathRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new IsFieldsHasContextPathRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheIsFieldSelectionHasNoContextPath()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Query {
                productById(id: ID! @is(field: "id")): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheIsFieldSelectionHasAContextPath()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    tenantById(tenant: ID! @is(field: "$tenantContext.id")): Tenant @lookup
                }

                type Tenant @context(name: "tenantContext") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.tenantById(tenant:)' in schema 'A' contains a context path. A context path is only allowed in the 'field' argument of the @require directive.",
                    "code": "IS_FIELDS_HAS_CONTEXT_PATH",
                    "severity": "Error",
                    "coordinate": "Query.tenantById(tenant:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
