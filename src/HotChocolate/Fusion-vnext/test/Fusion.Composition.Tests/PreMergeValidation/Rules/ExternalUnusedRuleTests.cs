using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalUnusedRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator = new([new ExternalUnusedRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "EXTERNAL_UNUSED"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the `name` field is marked with @external and is used by the
            // @provides directive, satisfying the rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "name")
                    }
                    """
                ]
            },
            // Provides two fields.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "id name")
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
            // In this example, the `name` field is marked with @external but is not used by the
            // @provides directive, violating the rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        title: String @external
                        author: Author
                    }
                    """
                ],
                [
                    "External field 'Product.title' in schema 'A' is not referenced by an " +
                    "@provides directive in the schema."
                ]
            },
            // Provides different field.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        title: String @external
                        author: Author
                    }

                    type Query {
                        productByName(name: String): Product @provides(fields: "author")
                    }
                    """
                ],
                [
                    "External field 'Product.title' in schema 'A' is not referenced by an " +
                    "@provides directive in the schema."
                ]
            }
        };
    }
}
