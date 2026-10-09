namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class InvalidProjectedFieldSharingRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InvalidProjectedFieldSharingRule();

    // "Chair" implements two unrelated interfaces whose stand-ins both default "reviews", and both
    // declarations are marked @shareable, so the contract is intentionally identical.
    [Fact]
    public void Validate_UnrelatedDefaultsShareable_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface PhysicalProduct @key(fields: "id") {
                id: ID!
            }

            interface DigitalProduct @key(fields: "id") {
                id: ID!
            }

            type Chair implements PhysicalProduct & DigitalProduct @key(fields: "id") {
                id: ID!
            }
            """,
            """
            # Schema B
            type PhysicalProduct @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [Review!]! @shareable
            }

            type Review {
                rating: Int! @shareable
            }
            """,
            """
            # Schema C
            type DigitalProduct @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [Review!]! @shareable
            }

            type Review {
                rating: Int! @shareable
            }
            """
        ]);
    }

    // The same two unrelated defaults, neither marked @shareable, leave no single most-specific
    // default for "Chair.reviews".
    [Fact]
    public void Validate_UnrelatedDefaultsNotShareable_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface PhysicalProduct @key(fields: "id") {
                    id: ID!
                }

                interface DigitalProduct @key(fields: "id") {
                    id: ID!
                }

                type Chair implements PhysicalProduct & DigitalProduct @key(fields: "id") {
                    id: ID!
                }
                """,
                """
                # Schema B
                type PhysicalProduct @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int! @shareable
                }
                """,
                """
                # Schema C
                type DigitalProduct @interfaceObject @key(fields: "id") {
                    id: ID!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int! @shareable
                }
                """
            ],
            [
                """
                {
                    "message": "The default field 'reviews' on type 'Chair' is contributed by unrelated interfaces 'DigitalProduct', 'PhysicalProduct'; every contributing @interfaceObject declaration must be marked @shareable.",
                    "code": "INVALID_PROJECTED_FIELD_SHARING",
                    "severity": "Error",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
