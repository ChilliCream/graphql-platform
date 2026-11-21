namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ContextOnInterfaceObjectRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new ContextOnInterfaceObjectRule();

    [Fact]
    public void Validate_Should_Succeed_When_TheContextIsOnAnInterface()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Query {
                media: Media
            }

            interface Media @key(fields: "id") @context(name: "media") {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_TheInterfaceObjectHasNoContext()
    {
        // arrange & act & assert
        AssertValid(
        [
            """
            type Query {
                media: Media
            }

            type Media @key(fields: "id") @interfaceObject {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_TheInterfaceObjectHasAContext()
    {
        // arrange & act & assert
        AssertInvalid(
            [
                """
                type Query {
                    media: Media
                }

                type Media @key(fields: "id") @interfaceObject @context(name: "media") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                  "message": "The type 'Media' in schema 'A' is annotated with @interfaceObject and must not be annotated with @context.",
                  "code": "CONTEXT_ON_INTERFACE_OBJECT",
                  "severity": "Error",
                  "coordinate": "Media",
                  "member": "context",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }
}
