using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyInvalidSyntaxRuleTests
{
    private static readonly object s_rule = new KeyInvalidSyntaxRule();
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
        Assert.True(_log.All(e => e.Code == "KEY_INVALID_SYNTAX"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument is a correctly formed selection set:
            // "sku featuredItem { id }" is properly balanced and contains no syntax errors.
            {
                [
                    """
                    type Product @key(fields: "sku featuredItem { id }") {
                        sku: String!
                        featuredItem: Node!
                    }

                    interface Node {
                        id: ID!
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
            // Here, the selection set "featuredItem { id" is missing the closing brace "}". It is
            // thus invalid syntax, causing a KEY_INVALID_SYNTAX error.
            {
                [
                    """
                    type Product @key(fields: "featuredItem { id") {
                        featuredItem: Node!
                        sku: String!
                    }

                    interface Node {
                        id: ID!
                    }
                    """
                ],
                [
                    "A @key directive on type 'Product' in schema 'A' contains invalid syntax in "
                    + "the 'fields' argument."
                ]
            }
        };
    }
}
