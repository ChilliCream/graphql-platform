using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class DisallowedInaccessibleElementsRuleTests
{
    private static readonly object s_rule = new DisallowedInaccessibleElementsRule();
    private static readonly ImmutableArray<object> s_rules = [s_rule];
    private readonly CompositionLog _log = new();

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(_log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(sdl);
        var validator = new SourceSchemaValidator(schemas, s_rules, _log);

        // act
        var result = validator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, _log.Select(e => e.Message).ToArray());
        Assert.True(_log.All(e => e.Code == "DISALLOWED_INACCESSIBLE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the "String" type is not marked as @inaccessible, which adheres to the rule.
            {
                [
                    """
                    type Product {
                        price: Float
                        name: String
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // In this example, the "String" scalar is marked as @inaccessible. This violates the
            // rule because "String" is a required built-in type that cannot be inaccessible.
            {
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
                ]
            },
            // In this example, the introspection type "__Type" is marked as @inaccessible. This
            // violates the rule because introspection types must remain accessible for GraphQL
            // introspection queries to work.
            {
                [
                    """
                    type __Type @inaccessible {
                        kind: __TypeKind!
                        name: String
                        fields(includeDeprecated: Boolean = false): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection type '__Type' in schema 'A' is not accessible."
                ]
            },
            // Inaccessible introspection field.
            {
                [
                    """
                    type __Type {
                        kind: __TypeKind! @inaccessible
                        name: String
                        fields(includeDeprecated: Boolean = false): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection field '__Type.kind' in schema 'A' is not accessible."
                ]
            },
            // Inaccessible introspection argument.
            {
                [
                    """
                    type __Type {
                        kind: __TypeKind!
                        name: String
                        fields(includeDeprecated: Boolean = false @inaccessible): [__Field!]
                    }
                    """
                ],
                [
                    "The introspection argument '__Type.fields(includeDeprecated:)' in schema "
                    + "'A' is not accessible."
                ]
            },
            // Inaccessible built-in directive argument.
            {
                [
                    """
                    directive @skip(if: Boolean! @inaccessible)
                        on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
                    """
                ],
                [
                    "The built-in directive argument '@skip(if:)' in schema 'A' is not accessible."
                ]
            }
        };
    }
}
