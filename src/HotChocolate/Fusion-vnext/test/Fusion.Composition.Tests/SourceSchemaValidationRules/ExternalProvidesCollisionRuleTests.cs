using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalProvidesCollisionRuleTests
{
    private static readonly object s_rule = new ExternalProvidesCollisionRule();
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
        Assert.True(_log.All(e => e.Code == "EXTERNAL_PROVIDES_COLLISION"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, "method" is only annotated with @external in Schema B, without any
            // other directive. This usage is valid.
            {
                [
                    """
                    # Source Schema A
                    type Payment {
                        id: ID!
                        method: String
                    }
                    """,
                    """
                    # Source Schema B
                    type Payment {
                        id: ID!
                        # This field is external, defined in Schema A.
                        method: String @external
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
            // In this example, "description" is annotated with @external and also with @provides.
            // Because @external and @provides cannot co-exist on the same field, an
            // EXTERNAL_PROVIDES_COLLISION error is produced.
            {
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
                    "The external field 'Invoice.description' in schema 'B' must not be annotated "
                    + "with the @provides directive."
                ]
            }
        };
    }
}
