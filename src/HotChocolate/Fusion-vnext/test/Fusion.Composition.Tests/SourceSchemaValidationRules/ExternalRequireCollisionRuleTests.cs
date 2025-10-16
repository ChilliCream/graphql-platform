namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalRequireCollisionRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalRequireCollisionRule();

    // In this example, "title" has arguments annotated with @require in Schema B, but is not marked
    // as @external. This usage is valid.
    [Fact]
    public void Validate_NoExternalRequireCollision_Succeeds()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Book {
                id: ID!
                title: String
                subtitle: String
            }
            """,
            """
            # Source Schema B
            type Book {
                id: ID!
                title(subtitle: String @require(field: "subtitle")): String
            }
            """
        ]);
    }

    // The following example is invalid, since "title" is marked with @external and has an argument
    // that is annotated with @require. This conflict leads to an EXTERNAL_REQUIRE_COLLISION error.
    [Fact]
    public void Validate_ExternalRequireCollision_Fails()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                type Book {
                    id: ID!
                    title: String
                    subtitle: String
                }
                """,
                """
                # Source Schema B
                type Book {
                    id: ID!
                    title(subtitle: String @require(field: "subtitle")): String @external
                }
                """
            ],
            [
                "The external field 'Book.title' in schema 'B' must not have arguments that are "
                + "annotated with the @require directive."
            ]);
    }
}
