using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;
using HotChocolate.Skimmed.Serialization;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class OutputFieldTypesMergeableRuleTests
{
    [Test]
    [MethodDataSource(nameof(ValidExamplesData))]
    public async Task Examples_Valid(string[] sdl)
    {
        // arrange
        var log = new CompositionLog();
        var context = new PreMergeValidationContext(
            new CompositionContext(sdl.Select(SchemaParser.Parse).ToArray(), log));

        context.Initialize();

        // act
        var result = new OutputFieldTypesMergeableRule().Run(context);

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
            new CompositionContext(sdl.Select(SchemaParser.Parse).ToArray(), log));

        context.Initialize();

        // act
        var result = new OutputFieldTypesMergeableRule().Run(context);

        // assert
        await Assert.That(result.IsFailure).IsTrue();
        await Assert.That(log.EntryCount).IsEqualTo(1);
        await Assert.That(log.Entries[0].Code).IsEqualTo("OUTPUT_FIELD_TYPES_NOT_MERGEABLE");
        await Assert.That(log.Entries[0].Severity).IsEqualTo(LogSeverity.Error);
    }

    public static IEnumerable<Func<string[]>> ValidExamplesData()
    {
        return
        [
            // Fields with the same type are mergeable.
            () =>
            [
                """
                type User {
                    birthdate: String
                }
                """,
                """
                type User {
                    birthdate: String
                }
                """
            ],
            // Fields with different nullability are mergeable, resulting in a merged field with a
            // nullable type.
            () =>
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: String
                }
                """
            ],
            () =>
            [
                """
                type User {
                    tags: [String!]
                }
                """,
                """
                type User {
                    tags: [String]!
                }
                """,
                """
                type User {
                    tags: [String]
                }
                """
            ]
        ];
    }

    public static IEnumerable<Func<string[]>> InvalidExamplesData()
    {
        return
        [
            // Fields are not mergeable if the named types are different in kind or name.
            () =>
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: DateTime!
                }
                """
            ],
            () =>
            [
                """
                type User {
                    tags: [Tag]
                }

                type Tag {
                    value: String
                }
                """,
                """
                type User {
                    tags: [Tag]
                }

                scalar Tag
                """
            ]
        ];
    }
}
