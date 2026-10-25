namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class ImplementWithoutDefaultRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ImplementWithoutDefaultRule();

    // "Product"'s stand-in contributes a default "weight"; "PhysicalProduct"'s stand-in replaces it
    // with @implement. The marker matches an applicable default, so composition succeeds.
    [Fact]
    public void Validate_ImplementMatchesAncestorDefault_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Product @key(fields: "id") {
                id: ID!
                name: String!
            }

            interface PhysicalProduct implements Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            """
            # Schema B
            type Product @interfaceObject @key(fields: "id") {
                id: ID!
                weight: Float!
            }
            """,
            """
            # Schema C
            type PhysicalProduct @interfaceObject @key(fields: "id") {
                id: ID!
                weight: Float! @implement
            }
            """
        ]);
    }

    // No source schema contributes a default "weight", so @implement on "PhysicalProduct.weight"
    // matches no default.
    [Fact]
    public void Validate_ImplementWithoutDefault_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Product @key(fields: "id") {
                    id: ID!
                    name: String!
                }

                interface PhysicalProduct implements Product @key(fields: "id") {
                    id: ID!
                    name: String!
                }
                """,
                """
                # Schema B
                type PhysicalProduct @interfaceObject @key(fields: "id") {
                    id: ID!
                    weight: Float! @implement
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'PhysicalProduct.weight' is marked with @implement in schema 'B', but no applicable default implementation exists for it.",
                    "code": "IMPLEMENT_WITHOUT_DEFAULT",
                    "severity": "Error",
                    "coordinate": "PhysicalProduct.weight",
                    "member": "weight",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
