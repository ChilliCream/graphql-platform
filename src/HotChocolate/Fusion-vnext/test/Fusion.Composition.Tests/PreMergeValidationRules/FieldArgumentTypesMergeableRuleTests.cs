namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class FieldArgumentTypesMergeableRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new FieldArgumentTypesMergeableRule();

    // Arguments with the same type are mergeable.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableSameType_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String): String
            }
            """,
            """
            type User {
                field(argument: String): String
            }
            """
        ]);
    }

    // Arguments that differ on nullability of an argument type are mergeable.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableDifferentNullability_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String!): String
            }
            """,
            """
            type User {
                field(argument: String): String
            }
            """
        ]);
    }

    // Arguments that differ on nullability of an argument list type are mergeable.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableDifferentNullabilityListTypes_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: [String!]): String
            }
            """,
            """
            type User {
                field(argument: [String]!): String
            }
            """,
            """
            type User {
                field(argument: [String]): String
            }
            """
        ]);
    }

    // The "User" type is inaccessible in schema B, so the argument will not be merged.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableOneTypeInaccessible_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String!): String
            }
            """,
            """
            type User @inaccessible {
                field(argument: DateTime): String
            }
            """
        ]);
    }

    // The "User" type is internal in schema B, so the argument will not be merged.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableOneTypeInternal_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String!): String
            }
            """,
            """
            type User @internal {
                field(argument: DateTime): String
            }
            """
        ]);
    }

    // The "field" field is inaccessible in schema B, so the argument will not be merged.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableOneFieldInaccessible_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String!): String
            }
            """,
            """
            type User {
                field(argument: DateTime): String @inaccessible
            }
            """
        ]);
    }

    // The "field" field is internal in schema B, so the argument will not be merged.
    [Fact]
    public void Validate_FieldArgumentTypesMergeableOneFieldInternal_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                field(argument: String!): String
            }
            """,
            """
            type User {
                field(argument: DateTime): String @internal
            }
            """
        ]);
    }

    // Arguments are not mergeable if the named types are different in kind or name.
    [Fact]
    public void Validate_FieldArgumentTypesNotMergeableDifferentTypes_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    field(argument: String!): String
                }
                """,
                """
                type User {
                    field(argument: DateTime): String
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'User.field(argument:)' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.field(argument:)",
                    "member": "argument",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Arguments are not mergeable if the element types are different in kind or name.
    [Fact]
    public void Validate_FieldArgumentTypesNotMergeableDifferentListTypes_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    field(argument: [String]): String
                }
                """,
                """
                type User {
                    field(argument: [DateTime]): String
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'User.field(argument:)' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.field(argument:)",
                    "member": "argument",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // More than two schemas.
    [Fact]
    public void Validate_FieldArgumentTypesNotMergeableTwice_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    field(argument: [String]): String
                }
                """,
                """
                type User {
                    field(argument: [DateTime]): String
                }
                """,
                """
                type User {
                    field(argument: [Int]): String
                }
                """
            ],
            [
                """
                {
                    "message": "The argument 'User.field(argument:)' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.field(argument:)",
                    "member": "argument",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "The argument 'User.field(argument:)' has a different type shape in schema 'B' than it does in schema 'C'.",
                    "code": "FIELD_ARGUMENT_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.field(argument:)",
                    "member": "argument",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
