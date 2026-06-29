namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class PropagateNullNotRepeatableRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new PropagateNullNotRepeatableRule();

    [Fact]
    public void Validate_SinglePropagateNullDirective_Succeeds()
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
    public void Validate_DuplicatePropagateNullDirective_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    productById(id: ID!): Product @lookup @propagateNull @propagateNull
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
                    "message": "The field 'Query.productById' in schema 'A' includes multiple @propagateNull directives, but @propagateNull is not repeatable.",
                    "code": "PROPAGATE_NULL_DIRECTIVE_NOT_REPEATABLE",
                    "severity": "Error",
                    "coordinate": "Query.productById",
                    "member": "productById",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
