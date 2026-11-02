namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class EmptyMergedUnionTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new EmptyMergedUnionTypeRule();

    // In the following example, the merged union type "SearchResult" is valid. It includes all
    // member types from both source schemas, with "User" being hidden due to the @inaccessible
    // directive in one of the source schemas.
    [Fact]
    public void Validate_NonEmptyMergedUnionTypeInaccessibleUnionMember_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            union SearchResult = User | Product

            type User @inaccessible {
                id: ID!
            }

            type Product {
                id: ID!
            }
            """,
            """
            # Schema B
            union SearchResult = Product | Order

            type Product {
                id: ID!
            }

            type Order {
                id: ID!
            }
            """
        ]);
    }

    // If the @inaccessible directive is applied to a union type itself, the entire merged union
    // type is excluded from the composite execution schema, and it is not required to contain any
    // members.
    [Fact]
    public void Validate_NonEmptyMergedUnionTypeInaccessibleUnionType_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            union SearchResult @inaccessible = User | Product

            type User {
                id: ID!
            }

            type Product {
                id: ID!
            }
            """,
            """
            # Schema B
            union SearchResult = Product | Order

            type Product {
                id: ID!
            }

            type Order {
                id: ID!
            }
            """
        ]);
    }

    // This example demonstrates an invalid merged union type. In this case, "SearchResult" is
    // defined in two source schemas, but all member types are marked as @inaccessible in at least
    // one of the source schemas, resulting in an empty merged union type.
    [Fact]
    public void Validate_EmptyMergedUnionTypeAllMembersInaccessible_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                union SearchResult = User | Product

                type User @inaccessible {
                    id: ID!
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                union SearchResult = User | Product

                type User {
                    id: ID!
                }

                type Product @inaccessible {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The merged union type 'SearchResult' is empty.",
                    "code": "EMPTY_MERGED_UNION_TYPE",
                    "severity": "Error",
                    "coordinate": "SearchResult",
                    "member": "SearchResult",
                    "schema": "default",
                    "extensions": {}
                }
                """
            ]);
    }
}
