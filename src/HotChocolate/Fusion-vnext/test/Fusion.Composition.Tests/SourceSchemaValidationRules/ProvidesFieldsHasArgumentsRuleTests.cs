namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ProvidesFieldsHasArgumentsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ProvidesFieldsHasArgumentsRule();

    // In this example, the "Article" type has a valid @provides directive that references the
    // argument-free field "tags".
    [Fact]
    public void Validate_ProvidesFieldsHasNoArguments_Succeeds()
    {
        AssertValid(
        [
            """
            type User @key(fields: "id") {
                id: ID!
                tags: [String]
            }

            type Article @key(fields: "id") {
                id: ID!
                author: User! @provides(fields: "tags")
            }
            """
        ]);
    }

    // This violates the rule because the "tags" field referenced in the "fields" argument of the
    // @provides directive is defined with arguments ("limit: UserType = ADMIN").
    [Fact]
    public void Validate_ProvidesFieldsHasArguments_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    tags(limit: UserType = ADMIN): [String]
                }

                enum UserType {
                    REGULAR
                    ADMIN
                }

                type Article @key(fields: "id") {
                    id: ID!
                    author: User! @provides(fields: "tags")
                }
                """
            ],
            [
                "The @provides directive on field 'Article.author' in schema 'A' references field "
                + "'User.tags', which must not have arguments."
            ]);
    }

    // Nested field.
    [Fact]
    public void Validate_ProvidesFieldsHasArgumentsNested_Fails()
    {
        AssertInvalid(
            [
                """
                type User @key(fields: "id") {
                    id: ID!
                    info: UserInfo
                }

                type UserInfo {
                    tags(limit: UserType = ADMIN): [String]
                }

                enum UserType {
                    REGULAR
                    ADMIN
                }

                type Article @key(fields: "id") {
                    id: ID!
                    author: User! @provides(fields: "info { tags }")
                }
                """
            ],
            [
                "The @provides directive on field 'Article.author' in schema 'A' references field "
                + "'UserInfo.tags', which must not have arguments."
            ]);
    }
}
