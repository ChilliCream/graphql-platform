namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyInvalidArgumentsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyInvalidArgumentsRule();

    // A key field with no arguments is valid.
    [Fact]
    public void Validate_KeyFieldsWithoutArguments_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id name") {
                id: ID!
                name: String
                tags: [String]
            }
            """
        ]);
    }

    // A key field with a valid constant argument is allowed.
    [Fact]
    public void Validate_KeyFieldWithValidConstantArgument_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id tags(first: 10)") {
                id: ID!
                tags(first: Int): [String]
            }
            """
        ]);
    }

    // A key field referencing an unknown argument is invalid.
    [Fact]
    public void Validate_KeyFieldWithUnknownArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id tags(limit: 10)") {
                    id: ID!
                    tags(first: Int): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The argument 'limit' does not exist on field 'User.tags'.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A key field supplying an incompatible argument value is invalid.
    [Fact]
    public void Validate_KeyFieldWithIncompatibleArgumentValue_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id tags(first: \"abc\")") {
                    id: ID!
                    tags(first: Int): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The value provided for argument 'first' on field 'User.tags' is not compatible with the type 'Int'.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A key field omitting a required argument is invalid.
    [Fact]
    public void Validate_KeyFieldMissingRequiredArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id tags") {
                    id: ID!
                    tags(first: Int!): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The required argument 'first' on field 'User.tags' was not provided.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A nested key field with an unknown argument is invalid (exercises recursion).
    [Fact]
    public void Validate_NestedKeyFieldWithUnknownArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id info { tags(limit: 10) }") {
                    id: ID!
                    info: UserInfo
                }

                type UserInfo {
                    tags(first: Int): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The argument 'limit' does not exist on field 'UserInfo.tags'.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A key field argument that uses a variable is invalid; arguments must be constant.
    [Fact]
    public void Validate_KeyFieldWithVariableArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id tags(first: $var)") {
                    id: ID!
                    tags(first: Int): [String]
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The value provided for argument 'first' on field 'User.tags' must be a constant value, not a variable.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A variable supplied to a custom scalar argument is also rejected. Custom scalars are otherwise
    // accepted leniently, so this guards against variables slipping through that path.
    [Fact]
    public void Validate_KeyFieldWithVariableArgumentOnCustomScalar_Fails()
    {
        AssertInvalid(
            [
                """
                scalar Scope

                type User @key(fields: "id(scope: $var)") {
                    id(scope: Scope): ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'User' in schema 'A' specifies invalid arguments. The value provided for argument 'scope' on field 'User.id' must be a constant value, not a variable.",
                    "code": "KEY_INVALID_ARGUMENTS",
                    "severity": "Error",
                    "coordinate": "User",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
