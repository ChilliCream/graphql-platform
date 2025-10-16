namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyInvalidFieldsTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyInvalidFieldsTypeRule();

    // In this example, the @key directive’s "fields" argument is the string "id uuid", identifying
    // two fields that form the object key. This usage is valid.
    [Fact]
    public void Validate_KeyValidFieldsType_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id uuid") {
                id: ID!
                uuid: ID!
                name: String
            }

            type Query {
                users: [User]
            }
            """
        ]);
    }

    // Here, the "fields" argument is provided as a boolean (true) instead of a string. This
    // violates the directive requirement and triggers a KEY_INVALID_FIELDS_TYPE error.
    [Fact]
    public void Validate_KeyInvalidFieldsType_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: true) {
                    id: ID
                }
                """
            ],
            [
                "A @key directive on type 'User' in schema 'A' must specify a string value for the "
                + "'fields' argument."
            ]);
    }

    // Multiple keys.
    [Fact]
    public void Validate_KeyInvalidFieldsTypeMultipleKeys_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: true) @key(fields: false) {
                    id: ID
                }
                """
            ],
            [
                "A @key directive on type 'User' in schema 'A' must specify a string value for the "
                + "'fields' argument.",

                "A @key directive on type 'User' in schema 'A' must specify a string value for the "
                + "'fields' argument."
            ]);
    }
}
