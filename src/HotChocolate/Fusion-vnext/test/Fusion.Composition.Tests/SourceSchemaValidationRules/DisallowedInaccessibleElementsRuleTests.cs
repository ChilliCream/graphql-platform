namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class DisallowedInaccessibleElementsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new DisallowedInaccessibleElementsRule();

    // Here, the "String" type is not marked as @inaccessible, which adheres to the rule.
    [Fact]
    public void Validate_AllowedAccessibleElements_Succeeds()
    {
        AssertValid(
        [
            """
            type Product {
                price: Float
                name: String
            }
            """
        ]);
    }

    // In this example, the "String" scalar is marked as @inaccessible. This violates the rule
    // because "String" is a required built-in type that cannot be inaccessible.
    [Fact]
    public void Validate_DisallowedInaccessibleElementsBuiltInScalar_Fails()
    {
        AssertInvalid(
            [
                """
                scalar String @inaccessible

                type Product {
                    price: Float
                    name: String
                }
                """
            ],
            [
                "The built-in scalar type 'String' in schema 'A' is not accessible."
            ]);
    }

    // Inaccessible built-in directive argument.
    [Fact]
    public void Validate_DisallowedInaccessibleElementsBuiltInDirectiveArgument_Fails()
    {
        AssertInvalid(
            [
                """
                directive @skip(if: Boolean! @inaccessible)
                    on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
                """
            ],
            [
                "The built-in directive argument '@skip(if:)' in schema 'A' is not accessible."
            ]);
    }

    // In this example, the introspection type "__Type" is marked as @inaccessible. This violates
    // the rule because introspection types must remain accessible for GraphQL introspection queries
    // to work.
    [Fact]
    public void Validate_DisallowedInaccessibleElementsIntrospectionType_Fails()
    {
        AssertInvalid(
            [
                """
                type __Type @inaccessible {
                    kind: __TypeKind!
                    name: String
                    fields(includeDeprecated: Boolean! = false): [__Field!]
                }
                """
            ],
            [
                "The introspection type '__Type' in schema 'A' is not accessible."
            ]);
    }

    // Inaccessible introspection field.
    [Fact]
    public void Validate_DisallowedInaccessibleElementsIntrospectionField_Fails()
    {
        AssertInvalid(
            [
                """
                type __Type {
                    kind: __TypeKind! @inaccessible
                    name: String
                    fields(includeDeprecated: Boolean! = false): [__Field!]
                }
                """
            ],
            [
                "The introspection field '__Type.kind' in schema 'A' is not accessible."
            ]);
    }

    // Inaccessible introspection argument.
    [Fact]
    public void Validate_DisallowedInaccessibleElementsIntrospectionArgument_Fails()
    {
        AssertInvalid(
            [
                """
                type __Type {
                    kind: __TypeKind!
                    name: String
                    fields(includeDeprecated: Boolean! = false @inaccessible): [__Field!]
                }
                """
            ],
            [
                "The introspection argument '__Type.fields(includeDeprecated:)' in schema 'A' is "
                + "not accessible."
            ]);
    }
}
