namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesInvalidFieldsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesInvalidFieldsRule();

    // In the following example, the @provides directive references a valid field ("hobbies") on the
    // "UserDetails" type.
    [Fact]
    public void Validate_ProvidesValidFields_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id") {
                id: ID!
                details: UserDetails @provides(fields: "hobbies")
            }

            type UserDetails {
                hobbies: [String]
            }
            """
        ]);
    }

    // In the following example, the @provides directive specifies a field named "unknownField"
    // which is not defined on "UserDetails". This raises a PROVIDES_INVALID_FIELDS error.
    [Fact]
    public void Validate_ProvidesInvalidFields_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    details: UserDetails @provides(fields: "unknownField")
                }

                type UserDetails {
                    hobbies: [String]
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'User.details' in schema 'A' specifies an invalid field selection.",
                    "code": "PROVIDES_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "User.details",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'UserDetails'."
                        ]
                    }
                }
                """
            ]);
    }

    // The canonical @provides shape: a field selected by @provides is @external on the providing
    // schema (delivered by construction). Membership holds, so this is a deliverable selection.
    [Fact]
    public void Validate_ProvidesExternalFieldMember_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                reviewById(id: ID!): Review
            }

            type Review @key(fields: "id") {
                id: ID!
                author: User @provides(fields: "name")
            }

            type User @key(fields: "id") {
                id: ID!
                name: String @external
            }
            """
        ]);
    }

    // A @provides selection is deliverable only if every selected field, at every depth, exists on
    // the type in the providing schema. Here the nested field "unknownField" does not exist on "Unit".
    [Fact]
    public void Validate_ProvidesNestedFieldDoesNotExist_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    productById(id: ID!): Product
                }

                type Product @key(fields: "id") {
                    id: ID!
                    dimension: Dimension @provides(fields: "unit { unknownField }")
                }

                type Dimension {
                    unit: Unit
                }

                type Unit {
                    code: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'Product.dimension' in schema 'A' specifies an invalid field selection.",
                    "code": "PROVIDES_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Product.dimension",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'Unit'."
                        ]
                    }
                }
                """
            ]);
    }

    // The same membership rule applies inside an inline fragment: the fragment is traversed and the
    // field "unknownField" selected within it must exist on "Info".
    [Fact]
    public void Validate_ProvidesInlineFragmentFieldDoesNotExist_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    productById(id: ID!): Product
                }

                type Product @key(fields: "id") {
                    id: ID!
                    info: Info @provides(fields: "... on Info { unknownField }")
                }

                type Info {
                    name: String
                }
                """
            ],
            [
                """
                {
                    "message": "The @provides directive on field 'Product.info' in schema 'A' specifies an invalid field selection.",
                    "code": "PROVIDES_INVALID_FIELDS",
                    "severity": "Error",
                    "coordinate": "Product.info",
                    "member": "provides",
                    "schema": "A",
                    "extensions": {
                        "errors": [
                            "The field 'unknownField' does not exist on the type 'Info'."
                        ]
                    }
                }
                """
            ]);
    }
}
