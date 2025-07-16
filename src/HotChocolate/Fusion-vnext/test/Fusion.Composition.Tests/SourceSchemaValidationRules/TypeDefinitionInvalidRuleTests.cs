using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class TypeDefinitionInvalidRuleTests
{
    private static readonly object s_rule = new TypeDefinitionInvalidRule();
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
        Assert.True(_log.All(e => e.Code == "TYPE_DEFINITION_INVALID"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In the following example, the @key directive includes an additional argument,
            // "futureArg", which is not part of the specification. This is valid and allows the
            // directive to evolve without breaking existing schemas.
            {
                [
                    """
                    directive @key(
                        fields: FieldSelectionSet!
                        futureArg: String
                    ) repeatable on OBJECT | INTERFACE
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // In the following example, FieldSelectionMap is declared as an input type instead of
            // the required scalar. This leads to a TYPE_DEFINITION_INVALID error because the
            // defined scalar FieldSelectionMap is being overridden by an incompatible definition.
            {
                [
                    """
                    directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION

                    input FieldSelectionMap {
                        fields: [String!]!
                    }
                    """
                ],
                [
                    "The type or directive 'FieldSelectionMap' in schema 'A' is incompatible "
                    + "with the built-in type or directive of the same name.",

                    "The type or directive 'require' in schema 'A' is incompatible with the "
                    + "built-in type or directive of the same name."
                ]
            },
            // However, if the @key directive is defined without the required fields argument, as
            // shown below, it results in a TYPE_DEFINITION_INVALID error.
            {
                [
                    "directive @key(futureArg: String) repeatable on OBJECT | INTERFACE"
                ],
                [
                    "The type or directive 'key' in schema 'A' is incompatible with the built-in "
                    + "type or directive of the same name."
                ]
            },
            // Incompatible FieldSelectionSet scalar.
            {
                [
                    """
                    input FieldSelectionSet {
                        fields: [String!]!
                    }
                    """
                ],
                [
                    "The type or directive 'FieldSelectionSet' in schema 'A' is incompatible "
                    + "with the built-in type or directive of the same name."
                ]
            }
        };
    }
}
