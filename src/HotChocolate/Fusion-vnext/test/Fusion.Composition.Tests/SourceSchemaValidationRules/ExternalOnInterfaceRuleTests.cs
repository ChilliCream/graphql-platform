using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalOnInterfaceRuleTests
{
    private static readonly object s_rule = new ExternalOnInterfaceRule();
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
        Assert.True(_log.All(e => e.Code == "EXTERNAL_ON_INTERFACE"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the interface "Node" merely describes the field "id". Object types "User" and
            // "Product" implement and resolve "id". No @external usage occurs on the interface
            // itself, so no error is triggered.
            {
                [
                    """
                    interface Node {
                        id: ID!
                    }

                    type User implements Node {
                        id: ID!
                        name: String
                    }

                    type Product implements Node {
                        id: ID!
                        price: Int
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
            // Since "id" is declared on an interface and marked with @external, the composition
            // fails with EXTERNAL_ON_INTERFACE. An interface does not own the concrete field
            // resolution, so it is invalid to mark any of its fields as external.
            {
                [
                    """
                    interface Node {
                        id: ID! @external
                    }
                    """
                ],
                [
                    "The interface field 'Node.id' in schema 'A' must not be marked as external."
                ]
            }
        };
    }
}
