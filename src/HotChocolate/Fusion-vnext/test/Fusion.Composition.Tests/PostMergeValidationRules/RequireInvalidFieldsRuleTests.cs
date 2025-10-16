namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class RequireInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new RequireInvalidFieldsRule();

    // In the following example, the @require directive's "field" argument is a valid selection set
    // and satisfies the rule.
    [Fact]
    public void Validate_RequireValidFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type User @key(fields: "id") {
                id: ID!
                profile(name: String @require(field: "name")): Profile
            }

            type Profile {
                id: ID!
                name: String
            }
            """,
            """
            # Schema B
            type User @key(fields: "id") {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In this example, the "field" argument references fields from another source schema.
    [Fact]
    public void Validate_RequireValidFieldsAcrossSchemas_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                id: ID!
                delivery(
                    zip: String!
                    dimension: ProductDimensionInput!
                        @require(field: "{ productSize: dimension.size, productWeight: dimension.weight }")
                ): DeliveryEstimates
            }

            input ProductDimensionInput {
                productSize: Int!
                productWeight: Int!
            }
            """,
            """
            type Product {
                dimension: ProductDimension!
            }

            type ProductDimension {
                size: Int!
                weight: Int!
            }
            """
        ]);
    }

    // In the following example, the @require directive's "field" argument references a field with a
    // list type.
    [Fact]
    public void Validate_RequireValidFieldsListType_Succeeds()
    {
        AssertValid(
        [
            """
            type CartDiscount {
                channels(channelIds: [ID!]! @require(field: "channelIds")): ChannelConnection!
                id: ID!
            }
            """,
            """
            type CartDiscount {
               id: ID!
               name: String!
               channelIds: [ID!]!
            }
            """
        ]);
    }

    // In this example, the @require directive references a field ("unknownField") that does not
    // exist on the parent type ("Book"), causing a REQUIRE_INVALID_FIELDS error.
    [Fact]
    public void Validate_RequireInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    pages(pageSize: Int @require(field: "unknownField")): Int
                }
                """
            ],
            [
                "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                + "specifies an invalid field selection against the composed schema."
            ]);
    }

    // In the following example, the @require directive references a field from itself (Book.size)
    // which is not allowed. This results in a REQUIRE_INVALID_FIELDS error.
    [Fact]
    public void Validate_RequireInvalidFieldsReferenceToLocalField_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    size: Int
                    pages(pageSize: Int @require(field: "size")): Int
                }
                """
            ],
            [
                "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                + "specifies an invalid field selection against the composed schema."
            ]);
    }

    // Referencing a field that exists in both the current schema and another schema.
    [Fact]
    public void Validate_RequireInvalidFieldsReferenceToLocalAndRemoteField_Fails()
    {
        AssertInvalid(
            [
                """
                type Book {
                    id: ID!
                    size: Int
                    pages(pageSize: Int @require(field: "size")): Int
                }
                """,
                """
                type Book {
                    id: ID!
                    size: Int
                }
                """
            ],
            [
                "The @require directive on argument 'Book.pages(pageSize:)' in schema 'A' "
                + "specifies an invalid field selection against the composed schema."
            ]);
    }
}
