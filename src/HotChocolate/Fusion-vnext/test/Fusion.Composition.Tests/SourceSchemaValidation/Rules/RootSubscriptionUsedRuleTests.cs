using System.Collections.Immutable;
using HotChocolate.Fusion.Logging;

namespace HotChocolate.Fusion.SourceSchemaValidation.Rules;

public sealed class RootSubscriptionUsedRuleTests : CompositionTestBase
{
    private static readonly object s_rule = new RootSubscriptionUsedRule();
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
        Assert.True(_log.All(e => e.Code == "ROOT_SUBSCRIPTION_USED"));
        Assert.True(_log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Valid example.
            {
                [
                    """
                    schema {
                        subscription: Subscription
                    }

                    type Subscription {
                        productCreated: Product
                    }

                    type Product {
                        id: ID!
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
            // The following example violates the rule because "RootSubscription" is used as the
            // root subscription type, but a type named "Subscription" is also defined.
            {
                [
                    """
                    schema {
                        subscription: RootSubscription
                    }

                    type RootSubscription {
                        productCreated: Product
                    }

                    type Subscription {
                        deprecatedField: String
                    }
                    """
                ],
                [
                    "The root subscription type in schema 'A' must be named 'Subscription'."
                ]
            },
            // A type named "Subscription" is not the root subscription type.
            {
                [
                    "scalar Subscription"
                ],
                [
                    "The root subscription type in schema 'A' must be named 'Subscription'."
                ]
            }
        };
    }
}
