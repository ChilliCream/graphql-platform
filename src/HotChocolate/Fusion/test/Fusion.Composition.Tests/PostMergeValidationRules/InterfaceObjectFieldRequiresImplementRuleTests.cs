namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class InterfaceObjectFieldRequiresImplementRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new InterfaceObjectFieldRequiresImplementRule();

    // "Book" declares no "reviews" of its own, so it adopts the default contributed by the stand-in.
    [Fact]
    public void Validate_ImplementingTypeAdoptsDefault_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Media @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Book implements Media @key(fields: "id") {
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
                rating: Int!
            }
            """
        ]);
    }

    // "Book" declares its own "reviews" and marks it @implement, so the explicit replacement is
    // allowed.
    [Fact]
    public void Validate_ExplicitImplementMarked_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            interface Media @key(fields: "id") {
                id: ID!
                title: String!
            }

            type Book implements Media @key(fields: "id") {
                id: ID!
                title: String!
                reviews: [Review!]! @implement
            }

            type Review {
                rating: Int! @shareable
            }
            """,
            """
            # Schema B
            type Media @interfaceObject @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                rating: Int! @shareable
            }
            """
        ]);
    }

    // "Book.reviews" is declared directly but not marked @implement, so it collides with the default
    // contributed by the "Media" stand-in.
    [Fact]
    public void Validate_UnmarkedCollision_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                interface Media @key(fields: "id") {
                    id: ID!
                    title: String!
                }

                type Book implements Media @key(fields: "id") {
                    id: ID!
                    title: String!
                    reviews: [Review!]!
                }

                type Review {
                    rating: Int! @shareable
                }
                """,
                """
                # Schema B
                type Media @interfaceObject @key(fields: "id") {
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
                    "message": "The field 'reviews' provided by schema 'A' replaces the default implementation contributed to interface 'Media'. Mark it with @implement to keep this implementation, or remove it to adopt the default.",
                    "code": "INTERFACE_OBJECT_FIELD_REQUIRES_IMPLEMENT",
                    "severity": "Error",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // "PhysicalProduct" implements "Product", and both have stand-ins that default "weight".
    // "PhysicalProduct"'s stand-in declares "weight" without @implement, colliding with the default
    // contributed by "Product"'s stand-in, one level up the interface hierarchy.
    [Fact]
    public void Validate_MoreSpecificStandInUnmarked_Fails()
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
                type Product @interfaceObject @key(fields: "id") {
                    id: ID!
                    weight: Float!
                }
                """,
                """
                # Schema C
                type PhysicalProduct @interfaceObject @key(fields: "id") {
                    id: ID!
                    weight: Float!
                }
                """
            ],
            [
                """
                {
                    "message": "The field 'weight' provided by schema 'C' replaces the default implementation contributed to interface 'Product'. Mark it with @implement to keep this implementation, or remove it to adopt the default.",
                    "code": "INTERFACE_OBJECT_FIELD_REQUIRES_IMPLEMENT",
                    "severity": "Error",
                    "schema": "C",
                    "extensions": {}
                }
                """
            ]);
    }

    // The same hierarchy, but "PhysicalProduct"'s stand-in marks its "weight" field @implement, so the
    // more-specific default explicitly replaces the less-specific one.
    [Fact]
    public void Validate_MoreSpecificStandInImplement_Succeeds()
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
}
