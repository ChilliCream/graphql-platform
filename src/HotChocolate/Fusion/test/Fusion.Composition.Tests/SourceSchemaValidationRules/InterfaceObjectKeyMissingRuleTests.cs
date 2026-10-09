namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class InterfaceObjectKeyMissingRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InterfaceObjectKeyMissingRule();

    // The "Media" stand-in declares a @key, so it can be joined to the "Media" interface by "id".
    [Fact]
    public void Validate_StandInDeclaresKey_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Media @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                rating: Int!
            }
            """
        ]);
    }

    // The "Media" stand-in declares no @key, so the entity it stands in for cannot be resolved.
    [Fact]
    public void Validate_StandInMissingKey_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Media @interfaceObject {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    id: ID!
                    rating: Int!
                }
                """
            ],
            [
                """
                {
                    "message": "The @interfaceObject stand-in 'Media' in schema 'A' must declare at least one @key directive.",
                    "code": "INTERFACE_OBJECT_KEY_MISSING",
                    "severity": "Error",
                    "coordinate": "Media",
                    "member": "Media",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
