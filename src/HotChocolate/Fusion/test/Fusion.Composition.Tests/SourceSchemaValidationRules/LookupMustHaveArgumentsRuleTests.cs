namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class LookupMustHaveArgumentsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new LookupMustHaveArgumentsRule();

    // The "productById" lookup declares a single argument and is therefore valid.
    [Fact]
    public void Validate_LookupHasArguments_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // The "product" lookup declares no arguments and cannot resolve an entity.
    [Fact]
    public void Validate_LookupWithoutArguments_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    product: Product @lookup @internal
                }

                type Product {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The lookup field 'Query.product' in schema 'A' must declare at least one argument.",
                    "code": "LOOKUP_MUST_HAVE_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "Query.product",
                    "member": "product",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
