namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class PropagateNullOnNonLookupFieldRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new PropagateNullOnNonLookupFieldRule();

    [Fact]
    public void Validate_PropagateNullOnLookupField_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                productById(id: ID!): Product @lookup @propagateNull
            }

            type Product {
                id: ID!
                name: String
            }
            """
        ]);
    }

    [Fact]
    public void Validate_PropagateNullOnNonLookupField_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    product: Product @propagateNull
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
                    "message": "The field 'Query.product' in schema 'A' includes a @propagateNull directive, but is not a lookup field.",
                    "code": "PROPAGATE_NULL_ON_NON_LOOKUP_FIELD",
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
