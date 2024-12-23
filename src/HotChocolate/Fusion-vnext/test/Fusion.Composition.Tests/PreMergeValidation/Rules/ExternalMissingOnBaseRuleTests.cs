using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalMissingOnBaseRuleTests
{
    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalMissingOnBaseRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalMissingOnBaseRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(log);
        Assert.Equal("EXTERNAL_MISSING_ON_BASE", log.First().Code);
        Assert.Equal(LogSeverity.Error, log.First().Severity);
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, the `name` field on Product is defined in source schema A and marked as
            // @external in source schema B, which is valid because there is a base definition in
            // source schema A.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[]> InvalidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the `name` field on Product is marked as @external in source schema
            // B but has no non-@external declaration in any other source schema, violating the
            // rule.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ]
            },
            // The `name` field is external in both source schemas.
            {
                [
                    """
                    # Source schema A
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """,
                    """
                    # Source schema B
                    type Product {
                        id: ID
                        name: String @external
                    }
                    """
                ]
            }
        };
    }
}
