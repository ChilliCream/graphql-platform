using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalMissingOnBaseRuleTests
{
    [Test]
    [MethodDataSource(nameof(ValidExamplesData))]
    public async Task Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalMissingOnBaseRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(log.IsEmpty).IsTrue();
    }

    [Test]
    [MethodDataSource(nameof(InvalidExamplesData))]
    public async Task Examples_Invalid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalMissingOnBaseRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(log.Count()).IsEqualTo(1);
        await Assert.That(log.First().Code).IsEqualTo("EXTERNAL_MISSING_ON_BASE");
        await Assert.That(log.First().Severity).IsEqualTo(LogSeverity.Error);
    }

    public static IEnumerable<Func<string[]>> ValidExamplesData()
    {
        return
        [
            // Here, the `name` field on Product is defined in source schema A and marked as
            // @external in source schema B, which is valid because there is a base definition in
            // source schema A.
            () =>
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
        ];
    }

    public static IEnumerable<Func<string[]>> InvalidExamplesData()
    {
        return
        [
            // In this example, the `name` field on Product is marked as @external in source schema
            // B but has no non-@external declaration in any other source schema, violating the
            // rule.
            () =>
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
            ],
            // The `name` field is external in both source schemas.
            () =>
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
        ];
    }
}
