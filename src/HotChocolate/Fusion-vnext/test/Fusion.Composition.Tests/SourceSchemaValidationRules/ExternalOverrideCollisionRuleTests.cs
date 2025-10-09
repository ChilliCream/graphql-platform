using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class ExternalOverrideCollisionRuleTests
{
    private static readonly object s_rule = new ExternalOverrideCollisionRule();
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
        Assert.True(_log.All(e => e.Code == "EXTERNAL_OVERRIDE_COLLISION"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this scenario, "User.fullName" is overriding the field from schema A. Since
            // @override is not combined with @external on the same field, no collision occurs.
            {
                [
                    """
                    type User {
                        id: ID!
                        fullName: String @override(from: "A")
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
            // Here, "amount" is marked with both @override and @external. This violates the rule
            // because the field is simultaneously labeled as "override from another schema" and
            // "external" in the local schema, producing an EXTERNAL_OVERRIDE_COLLISION error.
            {
                [
                    """
                    type Payment {
                        id: ID!
                        amount: Int @override(from: "A") @external
                    }
                    """
                ],
                [
                    "The external field 'Payment.amount' must not be annotated with the @override directive."
                ]
            }
        };
    }
}
