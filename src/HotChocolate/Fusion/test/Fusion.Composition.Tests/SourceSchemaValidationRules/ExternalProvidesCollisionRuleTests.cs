namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalProvidesCollisionRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ExternalProvidesCollisionRule();

    // In this example, "description" is only annotated with @provides in Schema B, without any
    // other directive. This usage is valid.
    [Fact]
    public void Validate_NoExternalProvidesCollision_Succeeds()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Invoice {
                id: ID!
                description: String
            }
            """,
            """
            # Source Schema B
            type Invoice {
                id: ID!
                description: String @provides(fields: "length")
            }
            """
        ]);
    }

    // In this example, "description" is annotated with @external and also with @provides. Because
    // @external and @provides cannot co-exist on the same field, an EXTERNAL_PROVIDES_COLLISION
    // error is produced.
    [Fact]
    public void Validate_ExternalProvidesCollision_Fails()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                type Invoice {
                    id: ID!
                    description: String
                }
                """,
                """
                # Source Schema B
                type Invoice {
                    id: ID!
                    description: String @external @provides(fields: "length")
                }
                """
            ],
            [
                """
                {
                    "message": "The external field 'Invoice.description' in schema 'B' must not be annotated with the @provides directive.",
                    "code": "EXTERNAL_PROVIDES_COLLISION",
                    "severity": "Error",
                    "coordinate": "Invoice.description",
                    "member": "description",
                    "schema": "B",
                    "extensions": {}
                }
                """
            ]);
    }
}
