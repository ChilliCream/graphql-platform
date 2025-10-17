namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class IsInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new IsInvalidFieldsRule();

    // In the following example, the @is directive's "field" argument is a valid field selection map
    // and satisfies the rule.
    [Fact]
    public void Validate_IsValidFields_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                personById(id: ID! @is(field: "id")): Person @lookup
            }

            type Person {
                id: ID!
                name: String
            }
            """
        ]);
    }

    // In this example, the "field" argument references a field from another source schema.
    [Fact]
    public void Validate_IsValidFieldsAcrossSchemas_Succeeds()
    {
        AssertValid(
        [
            """
            # Schema A
            type Query {
                personByName(name: String! @is(field: "name")): Person @lookup
            }

            type Person {
                id: ID!
            }
            """,
            """
            # Schema B
            type Person {
                id: ID!
                name: String!
            }
            """
        ]);
    }

    // In this example, the @is directive references a field ("unknownField") that does not exist on
    // the return type ("Person"), causing an IS_INVALID_FIELDS error.
    [Fact]
    public void Validate_IsInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personById(id: ID! @is(field: "unknownField")): Person @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personById(id:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'Person'."
                        ]
                    }
                }
                """
            ]);
    }

    // In this example, the @is directive references a field ("unknownField") with a selection set,
    // that does not exist on the return type ("Person"), causing an IS_INVALID_FIELDS error.
    [Fact]
    public void Validate_IsInvalidFieldsWithSelectionSet_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personById(id: ID! @is(field: "unknownField.something")): Person @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personById(id:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'Person'."
                        ]
                    }
                }
                """
            ]);
    }

    // In this example, the @is directive references a field ("id") via a missing concrete type
    // ("SpecialPerson"), causing an IS_INVALID_FIELDS error.
    [Fact]
    public void Validate_IsInvalidFieldsMissingConcreteType_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personById(id: ID! @is(field: "<SpecialPerson>.id")): Person @lookup
                }

                interface Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personById(id:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The type condition in path '<SpecialPerson>.id' is invalid. Type 'SpecialPerson' does not exist."
                        ]
                    }
                }
                """
            ]);
    }

    // Type of argument does not match field type.
    [Fact]
    public void Validate_IsInvalidFieldsTypeMismatch_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personById(id: Int! @is(field: "id")): Person @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personById(id:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'id' is of type 'ID!' instead of the expected input type 'Int!'."
                        ]
                    }
                }
                """
            ]);
    }

    // List output type, singular input type.
    [Fact]
    public void Validate_IsInvalidFieldsListVsSingular_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personsById(id: ID! @is(field: "id")): [Person!]! @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personsById(id:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personsById(id:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "Expected the parent type of field 'id' to be a complex type but received '[Person!]!'."
                        ]
                    }
                }
                """
            ]);
    }

    // Singular output type, List input type.
    [Fact]
    public void Validate_IsInvalidFieldsSingularVsList_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personByIds(ids: [ID!]! @is(field: "id")): Person! @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personByIds(ids:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personByIds(ids:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'id' is of type 'ID!' instead of the expected input type '[ID!]!'."
                        ]
                    }
                }
                """
            ]);
    }

    // List output type, List input type (valid for Fusion v1 batch lookups).
    [Fact]
    public void Validate_IsInvalidFieldsListVsList_Fails()
    {
        AssertInvalid(
            [
                """
                # Schema A
                type Query {
                    personsByIds(ids: [ID!]! @is(field: "id")): [Person!]! @lookup
                }

                type Person {
                    id: ID!
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @is directive on argument 'Query.personsByIds(ids:)' in schema 'A' specifies an invalid field selection against the composed schema.",
                    "code": "IS_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Query.personsByIds(ids:)",
                    "member": "is",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "Expected the parent type of field 'id' to be a complex type but received '[Person!]!'."
                        ]
                    }
                }
                """
            ]);
    }
}
