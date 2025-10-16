namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class OutputFieldTypesMergeableRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new OutputFieldTypesMergeableRule();

    // Fields with the same type are mergeable.
    [Fact]
    public void Validate_OutputFieldTypesMergeable_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                birthdate: String
            }
            """,
            """
            type User {
                birthdate: String
            }
            """
        ]);
    }

    // Fields with different nullability are mergeable, resulting in a merged field with a nullable
    // type.
    [Fact]
    public void Validate_OutputFieldTypesMergeableDifferentNullability_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                birthdate: String!
            }
            """,
            """
            type User {
                birthdate: String
            }
            """
        ]);
    }

    // Fields that differ on nullability of a field list type are mergeable.
    [Fact]
    public void Validate_OutputFieldTypesMergeableDifferentNullabilityListTypes_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                tags: [String!]
            }
            """,
            """
            type User {
                tags: [String]!
            }
            """,
            """
            type User {
                tags: [String]
            }
            """
        ]);
    }

    // Fields are not mergeable if the named types are different in kind or name.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableDifferentNamedTypes_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: DateTime!
                }
                """
            ],
            [
                "The output field 'User.birthdate' has a different type shape in schema 'A' than "
                + "it does in schema 'B'."
            ]);
    }

    // Fields are not mergeable if the list type elements are different in kind or name.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableDifferentListElementKind_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    tags: [Tag]
                }

                type Tag {
                    value: String
                }
                """,
                """
                type User {
                    tags: [Tag]
                }

                scalar Tag
                """
            ],
            [
                "The output field 'User.tags' has a different type shape in schema 'A' than it "
                + "does in schema 'B'."
            ]);
    }

    // More than two schemas.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableTwice_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: DateTime!
                }
                """,
                """
                type User {
                    birthdate: Int!
                }
                """
            ],
            [
                "The output field 'User.birthdate' has a different type shape in schema 'A' than "
                + "it does in schema 'B'.",

                "The output field 'User.birthdate' has a different type shape in schema 'B' than "
                + "it does in schema 'C'."
            ]);
    }
}
