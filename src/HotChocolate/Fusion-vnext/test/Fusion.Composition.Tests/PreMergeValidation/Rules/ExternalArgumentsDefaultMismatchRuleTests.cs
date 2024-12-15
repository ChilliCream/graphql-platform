using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ExternalArgumentsDefaultMismatchRuleTests
{
    [Test]
    [MethodDataSource(nameof(ValidExamplesData))]
    public async Task Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log);
        var preMergeValidator = new PreMergeValidator([new ExternalArgumentsDefaultMismatchRule()]);

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
        var preMergeValidator = new PreMergeValidator([new ExternalArgumentsDefaultMismatchRule()]);

        // act
        var result = preMergeValidator.Validate(context);

        // assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(log.Count()).IsEqualTo(1);
        await Assert.That(log.First().Code).IsEqualTo("EXTERNAL_ARGUMENT_DEFAULT_MISMATCH");
        await Assert.That(log.First().Severity).IsEqualTo(LogSeverity.Error);
    }

    public static IEnumerable<Func<string[]>> ValidExamplesData()
    {
        return
        [
            // Fields with the same arguments are mergeable.
            () =>
            [
                """
                type Product {
                  name(language: String = "en"): String
                }
                """,
                """
                type Product {
                  name(language: String = "en"): String @external
                }
                """
            ],
        ];
    }

    public static IEnumerable<Func<string[]>> InvalidExamplesData()
    {
        return
        [
            // Fields are not mergeable if the default arguments do not match
            () =>
            [
                """
                type Product {
                  name(language: String = "en"): String
                }
                """,
                """
                type Product {
                  name(language: String = "de"): String @external
                }
                """
            ],
            () =>
            [
                """
                type Product {
                  name(language: String = "en"): String
                }
                """,
                """
                type Product {
                  name(language: String): String @external
                }
                """
            ]
        ];
    }
}
