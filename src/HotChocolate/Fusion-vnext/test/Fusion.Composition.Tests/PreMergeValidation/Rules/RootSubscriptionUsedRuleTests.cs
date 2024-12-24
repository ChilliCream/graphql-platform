using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class RootSubscriptionUsedRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new RootSubscriptionUsedRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(context.Log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "ROOT_SUBSCRIPTION_USED"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
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
            // The following example violates the rule because `RootSubscription` is used as the
            // root subscription type, but a type named `Subscription` is also defined.
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
            }
        };
    }
}
