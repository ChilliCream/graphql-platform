namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class InterfaceObjectNoInterfaceRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InterfaceObjectNoInterfaceRule();

    // Source schema A defines "Media" as a real interface, so source schema B's stand-in is valid.
    [Fact]
    public void Validate_StandInHasInterface_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Media {
                id: ID!
                title: String!
            }

            type Book implements Media {
                id: ID!
                title: String!
            }
            """,
            """
            # Schema B
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

    // Both schemas declare "Media" as a stand-in, but no schema defines it as an interface.
    [Fact]
    public void Validate_StandInWithoutInterface_Fails()
    {
        AssertInvalid(
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
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
                    id: ID!
                    averageRating: Float!
                }
                """
            ],
            [
                """
                {
                    "message": "The type 'Media' is declared as an @interfaceObject stand-in, but no source schema defines 'Media' as an interface.",
                    "code": "INTERFACE_OBJECT_NO_INTERFACE",
                    "severity": "Error",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
