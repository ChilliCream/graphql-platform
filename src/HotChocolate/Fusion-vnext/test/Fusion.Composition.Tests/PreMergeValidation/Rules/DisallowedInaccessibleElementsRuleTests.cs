using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class DisallowedInaccessibleElementsRuleTests
{
    [Test]
    [MethodDataSource(nameof(ValidExamplesData))]
    public async Task Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new PreMergeValidationContext(
            new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log));

        context.Initialize();

        // act
        var result = new DisallowedInaccessibleElementsRule().Run(context);

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
        var context = new PreMergeValidationContext(
            new CompositionContext([.. sdl.Select(SchemaParser.Parse)], log));

        context.Initialize();

        // act
        var result = new DisallowedInaccessibleElementsRule().Run(context);

        // assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(log.Count()).IsEqualTo(1);
        await Assert.That(log.First().Code).IsEqualTo("DISALLOWED_INACCESSIBLE");
        await Assert.That(log.First().Severity).IsEqualTo(LogSeverity.Error);
    }

    public static IEnumerable<Func<string[]>> ValidExamplesData()
    {
        return
        [
            // Here, the String type is not marked as @inaccessible, which adheres to the rule.
            () =>
            [
                """
                type Product {
                    price: Float
                    name: String
                }
                """
            ]
        ];
    }

    public static IEnumerable<Func<string[]>> InvalidExamplesData()
    {
        return
        [
            // In this example, the String scalar is marked as @inaccessible. This violates the rule
            // because String is a required built-in type that cannot be inaccessible.
            () =>
            [
                """
                scalar String @inaccessible

                type Product {
                    price: Float
                    name: String
                }
                """
            ],
            // In this example, the introspection type __Type is marked as @inaccessible. This
            // violates the rule because introspection types must remain accessible for GraphQL
            // introspection queries to work.
            () =>
            [
                """
                type __Type @inaccessible {
                    kind: __TypeKind!
                    name: String
                    fields(includeDeprecated: Boolean = false): [__Field!]
                }
                """
            ],
            // Inaccessible introspection field.
            () =>
            [
                """
                type __Type {
                    kind: __TypeKind! @inaccessible
                    name: String
                    fields(includeDeprecated: Boolean = false): [__Field!]
                }
                """
            ],
            // Inaccessible introspection argument.
            () =>
            [
                """
                type __Type {
                    kind: __TypeKind!
                    name: String
                    fields(includeDeprecated: Boolean = false @inaccessible): [__Field!]
                }
                """
            ],
            // Inaccessible built-in directive argument.
            () =>
            [
                """
                directive @skip(if: Boolean! @inaccessible)
                    on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
                """
            ]
        ];
    }
}
